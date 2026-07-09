using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using eiibd26.Firma;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NINA_WorkerService.Data;
using NINA_WorkerService.Models;
using NINA_WorkerService.Services;

namespace NINA_WorkerService;

public class ScrapingWorker : BackgroundService
{
    private readonly ILogger<ScrapingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Identidad honesta del bot: mismo UA para el header HTTP y para evaluar robots.txt.
    private const string BotUserAgentProduct = "EIIBD-Indexer";
    private const string BotUserAgentFull = "EIIBD-Indexer/1.0 (+https://eiibd.com/bot)";

    public ScrapingWorker(ILogger<ScrapingWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScrapingWorker iniciado");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Eiibd26Context>();

        // Vocabulario EII (glosario de la BD del Web) para firmar externos con la
        // biblioteca compartida. Mismo filtro que el Web: Activo=1 AND relación Directa (1).
        var vocabNombres = await db.GlossaryTerms
            .AsNoTracking()
            .Where(t => t.Activo && t.MedicalRelationSuggestedId == 1)
            .Select(t => t.Nombre)
            .ToListAsync(stoppingToken);
        var vocab = FirmaCalculator.CompilarVocabulario(vocabNombres);
        _logger.LogInformation("Vocabulario EII cargado: {Count} términos (relación Directa) para firma de externos.", vocab.Count);
        if (vocab.Count == 0)
            _logger.LogWarning("Vocabulario EII vacío: los externos NO se firmarán en esta corrida.");

        // Traductor EN→ES (2C). Si no hay API key configurada, el inglés se deja sin firmar.
        var translator = scope.ServiceProvider.GetRequiredService<Services.ITranslationService>();
        _logger.LogInformation("Traducción EN→ES (Anthropic): {Estado}.",
            translator.Habilitado ? "habilitada" : "DESHABILITADA (sin API key) — inglés no se firmará");

        // Embeddings (Voyage, Fase 4). Multilingüe: embebe el texto ORIGINAL (inglés SIN traducir).
        // Independiente del vocabulario y del traductor. Solo se guarda el vector, nunca el texto.
        var voyage = scope.ServiceProvider.GetRequiredService<eiibd26.Voyage.IVoyageEmbeddingClient>();
        _logger.LogInformation("Embeddings (Voyage {Modelo}): {Estado}.",
            voyage.Modelo, voyage.Habilitado ? "habilitados" : "DESHABILITADOS (sin API key) — externos no se embeberán");

        // Fuentes desde fuentes.json (editable a mano, viaja junto al ejecutable).
        // Si falta o está mal formado: log claro y salida limpia (no crash).
        var fuentes = CargarFuentes();
        if (fuentes == null)
        {
            _logger.LogError("fuentes.json ausente o mal formado; se aborta la corrida sin cambios.");
            return;
        }
        var activas = fuentes.Where(f => f.Activo).ToList();
        if (activas.Count == 0)
        {
            _logger.LogWarning("No hay fuentes activas en fuentes.json; nada que indexar.");
            return;
        }
        _logger.LogInformation("Fuentes activas: {Count} de {Total}", activas.Count, fuentes.Count);

        // Un solo HttpClient (UA honesto) para todas las fuentes.
        using var httpClient = CrearHttpClient();

        // robots.txt: caché por host, compartida entre fuentes (se lee robots una sola vez por host).
        var robotsCache = new Dictionary<string, RobotsMatcher>(StringComparer.OrdinalIgnoreCase);

        foreach (var fuente in activas)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Validar la fuente; una config inválida se salta sin tumbar el resto.
            if (!EsFuenteValida(fuente, out var motivo))
            {
                _logger.LogWarning("Fuente '{Nombre}' inválida ({Motivo}); se salta.", fuente.Nombre, motivo);
                continue;
            }

            // DIAGNÓSTICO: confirmar cómo se deserializó cada campo clave del JSON.
            _logger.LogInformation(
                "Fuente '{Nombre}' cargada: usarSitemap={Usar}, sitemapUrl={Sitemap}, urlInicial={Inicial}, maxPages={MaxPages}, excluirSitemaps=[{Excluir}]",
                fuente.Nombre, fuente.UsarSitemap, fuente.SitemapUrl ?? "(null)", fuente.UrlInicial, fuente.MaxPages,
                fuente.ExcluirSitemaps == null ? "" : string.Join(",", fuente.ExcluirSitemaps));

            // Aislar cada fuente: si una falla (red, robots, DB), se registra y se sigue con la siguiente.
            try
            {
                var site = await UpsertSourceSiteAsync(db, fuente, stoppingToken);
                await IndexarFuenteAsync(db, httpClient, robotsCache, site, fuente, vocab, translator, voyage, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fuente '{Nombre}' falló; se continúa con la siguiente.", fuente.Nombre);
            }
        }

        _logger.LogInformation("ScrapingWorker finalizado (ejecución única).");
    }

    // Carga y deserializa fuentes.json (System.Text.Json, case-insensitive; modo como enum de texto).
    // Devuelve null si el archivo falta o está mal formado (el llamador aborta limpio).
    private List<FuenteConfig>? CargarFuentes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fuentes.json");
        if (!File.Exists(path))
        {
            _logger.LogError("No existe fuentes.json en {Path}", path);
            return null;
        }
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<List<FuenteConfig>>(json, options) ?? new List<FuenteConfig>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fuentes.json mal formado ({Path})", path);
            return null;
        }
    }

    // Valida una fuente: urlInicial http(s) absoluta y hostPermitidos no vacío.
    private static bool EsFuenteValida(FuenteConfig fuente, out string motivo)
    {
        if (string.IsNullOrWhiteSpace(fuente.UrlInicial)
            || !Uri.TryCreate(fuente.UrlInicial, UriKind.Absolute, out var u)
            || (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
        {
            motivo = "urlInicial inválida";
            return false;
        }
        if (fuente.HostPermitidos == null || fuente.HostPermitidos.Count == 0)
        {
            motivo = "hostPermitidos vacío";
            return false;
        }
        motivo = string.Empty;
        return true;
    }

    private static HttpClient CrearHttpClient()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        if (!httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(BotUserAgentFull))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", BotUserAgentFull);
        // Accept explícito: algunos servidores/WAF (ej. funeiico) responden HTTP 406 sin él.
        // Incluye application/xml y */*, así los sitemaps y robots.txt siguen aceptándose.
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es,en;q=0.8");
        return httpClient;
    }

    // Upsert del SourceSite desde el JSON (el JSON es la fuente de verdad de los metadatos).
    private async Task<SourceSite> UpsertSourceSiteAsync(Eiibd26Context db, FuenteConfig fuente, CancellationToken ct)
    {
        var baseUrl = fuente.UrlPublica ?? fuente.UrlInicial;
        var site = await db.SourceSites.FirstOrDefaultAsync(s => s.BaseUrl == baseUrl, ct);

        if (site == null)
        {
            site = new SourceSite
            {
                Name = fuente.Nombre,
                BaseUrl = baseUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.SourceSites.Add(site);
        }
        else
        {
            site.Name = fuente.Nombre;
            site.IsActive = true;
            site.UpdatedAt = DateTime.UtcNow;
        }

        // Metadatos de fuente: siempre desde el JSON.
        site.Idioma = fuente.Idioma;
        site.Pais = fuente.Pais;
        site.Categoria = fuente.Categoria;
        site.UrlPublica = fuente.UrlPublica;

        await db.SaveChangesAsync(ct);
        return site;
    }

    // Indexa UNA fuente. URLs desde el sitemap (si usarSitemap) o, como fallback, por BFS de enlaces.
    // A cada URL se le aplica el flujo YA VALIDADO: robots + charset + filtro meta-description +
    // metadatos -> Article (ancla ScrapedPage, sin HTML). Parametrizado por la fuente.
    private async Task IndexarFuenteAsync(
        Eiibd26Context db, HttpClient httpClient, Dictionary<string, RobotsMatcher> robotsCache,
        SourceSite site, FuenteConfig fuente, IReadOnlyList<VocabularioTermino> vocab,
        Services.ITranslationService translator, eiibd26.Voyage.IVoyageEmbeddingClient voyage,
        CancellationToken stoppingToken)
    {
        var maxDepth = fuente.MaxDepth > 0 ? fuente.MaxDepth : 10;
        var maxPages = fuente.MaxPages > 0 ? fuente.MaxPages : 3000;
        var defaultLanguage = string.IsNullOrWhiteSpace(fuente.Idioma) ? "es" : fuente.Idioma!;

        // Español se firma directo (2B). Inglés se traduce y luego se firma (2C), si hay traductor.
        var esEspanol = defaultLanguage.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        var esIngles = defaultLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var firmarHabilitado = vocab.Count > 0 && (esEspanol || (esIngles && translator.Habilitado));
        // Embedding: multilingüe → aplica a CUALQUIER idioma sin traducir ni vocabulario.
        var embedHabilitado = voyage.Habilitado;
        var hostsPermitidos = new HashSet<string>(fuente.HostPermitidos, StringComparer.OrdinalIgnoreCase);
        // Restricción por sección (opcional): prefijos de path. Vacío = todo el host.
        var rutasPermitidas = fuente.RutasPermitidas ?? new List<string>();

        var startUri = new Uri(fuente.UrlInicial);
        var baseUri = new Uri($"{startUri.Scheme}://{startUri.Host}/");

        // Contadores de la fuente.
        var pagesProcessed = 0;
        var robotsSkipped = 0;
        var indexed = 0;
        var noMeta = 0;
        var errors = 0;
        var sitemapCount = 0;
        var firmadas = 0;
        var firmaOmitidaLastmod = 0;
        var traducidas = 0;
        var firmaErrores = 0;
        var embebidas = 0;
        var embedOmitidaLastmod = 0;
        var embedErrores = 0;

        // robots.txt: matcher propio + caché por host (compartida entre fuentes).
        async Task<RobotsMatcher> GetRobotsAsync(Uri pageUri)
        {
            if (!robotsCache.TryGetValue(pageUri.Host, out var matcher))
            {
                try
                {
                    var robotsUrl = $"{pageUri.Scheme}://{pageUri.Host}/robots.txt";
                    using var resp = await httpClient.GetAsync(robotsUrl, stoppingToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        var text = await resp.Content.ReadAsStringAsync(stoppingToken);
                        matcher = RobotsMatcher.Parse(text);
                    }
                    else
                    {
                        // Sin robots.txt (404) u otro estado -> se asume permitido.
                        _logger.LogInformation("robots.txt de {Host}: HTTP {Code} -> se asume permitido", pageUri.Host, (int)resp.StatusCode);
                        matcher = RobotsMatcher.AllowAll();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo leer robots.txt de {Host}; se asume permitido", pageUri.Host);
                    matcher = RobotsMatcher.AllowAll();
                }
                robotsCache[pageUri.Host] = matcher;
            }
            return matcher;
        }

        async Task DelayPoliteAsync(RobotsMatcher robots, CancellationToken ct)
        {
            double seconds = 1;
            var crawlDelay = robots.GetCrawlDelay(BotUserAgentProduct);
            if (crawlDelay.HasValue) seconds = Math.Max(1.0, crawlDelay.Value);
            await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
        }

        // Persiste el ancla (sin HTML) + Article para una URL con metadatos válidos.
        // publishedAt: lastmod del sitemap (si vino), guardado en ScrapedPage.PublishedAt.
        async Task PersistirAsync(string currentUrl,
            (string Title, string Snippet, string Language, string? MainTopic, string SnippetSource) meta, DateTime? publishedAt,
            string? textoPlano)
        {
            // Persistencia estilo índice (Opción A):
            //  - ScrapedPage = ANCLA de URL: solo Url + SourceSiteId; ContentRaw = "" (jamás el HTML).
            //  - Article = metadatos, colgado del ancla por ScrapedPageId; hereda la fuente vía el ancla.
            var anchor = await db.ScrapedPages
                .FirstOrDefaultAsync(p => p.Url == currentUrl && p.SourceSiteId == site.SourceSiteId, stoppingToken);

            // Estado previo para la optimización lastmod de la firma/embedding (antes de mutar el ancla).
            var esNuevo = anchor == null;
            var oldPublishedAt = anchor?.PublishedAt;
            var firmaExistente = anchor?.Firma;
            var embeddingExistente = anchor?.Embedding;

            if (anchor == null)
            {
                anchor = new ScrapedPage
                {
                    SourceSiteId = site.SourceSiteId,
                    Url = currentUrl,
                    TitleRaw = null,
                    ContentRaw = string.Empty,   // ANCLA: nunca se guarda el HTML del recurso externo
                    ContentText = null,
                    Language = meta.Language,
                    PublishedAt = publishedAt,   // lastmod del sitemap (uso futuro: re-indexar solo lo cambiado)
                    ScrapedAt = DateTime.UtcNow,
                    Status = "INDEXED"
                };
                db.ScrapedPages.Add(anchor);
                await db.SaveChangesAsync(stoppingToken); // obtener ScrapedPageId
            }
            else
            {
                anchor.ScrapedAt = DateTime.UtcNow;
                anchor.Status = "INDEXED";
                anchor.Language = meta.Language;
                anchor.ErrorMessage = null;
                if (publishedAt.HasValue) anchor.PublishedAt = publishedAt;
                await db.SaveChangesAsync(stoppingToken);
            }

            // Upsert del Article (dedup por URL vía el ancla): actualizar metadatos, no duplicar.
            var article = await db.Articles
                .FirstOrDefaultAsync(a => a.ScrapedPageId == anchor.ScrapedPageId, stoppingToken);

            if (article == null)
            {
                article = new Article
                {
                    ScrapedPageId = anchor.ScrapedPageId,
                    NormalizedTitle = meta.Title,
                    NormalizedContent = meta.Snippet,   // SOLO meta-descripción
                    Language = meta.Language,
                    MainTopic = meta.MainTopic,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                db.Articles.Add(article);
            }
            else
            {
                article.NormalizedTitle = meta.Title;
                article.NormalizedContent = meta.Snippet;
                article.Language = meta.Language;
                article.MainTopic = meta.MainTopic;
                article.UpdatedAt = DateTime.UtcNow;
                article.IsActive = true;
            }
            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation("Indexado ({Origen}) {Url} -> ArticleId {Id}", meta.SnippetSource, currentUrl, article.ArticleId);

            // FIRMA DE COBERTURA (solo fuentes en español en 2B). El texto se usó en memoria
            // (textoPlano) y aquí se calcula la firma con la biblioteca compartida; el texto NO
            // se persiste. Optimización lastmod: no re-firmar si ya hay firma y PublishedAt no cambió.
            if (firmarHabilitado)
            {
                var lastmodCambio = publishedAt.HasValue && oldPublishedAt != publishedAt;
                var necesitaFirma = esNuevo || firmaExistente == null || lastmodCambio;
                if (necesitaFirma)
                {
                    if (esIngles)
                    {
                        // Traducir EN→ES en memoria (título + cuerpo) y firmar la traducción.
                        // La traducción NUNCA se persiste. El título va en el texto para que sus
                        // conceptos cuenten en español; por eso se firma con título vacío.
                        var traducido = await translator.TraducirAEspanolAsync($"{meta.Title}. {textoPlano}", stoppingToken);
                        if (string.IsNullOrWhiteSpace(traducido))
                        {
                            firmaErrores++;
                            _logger.LogWarning("Traducción vacía/fallida; no se firma {Url}", currentUrl);
                        }
                        else
                        {
                            anchor.Firma = FirmaCalculator.Calcular(null, traducido, vocab);
                            anchor.FirmaCalculadaEn = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                            firmadas++;
                            traducidas++;
                            _logger.LogInformation("Traducido+firmado {Url}", currentUrl);
                        }
                    }
                    else
                    {
                        anchor.Firma = FirmaCalculator.Calcular(meta.Title, textoPlano, vocab);
                        anchor.FirmaCalculadaEn = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                        firmadas++;
                        _logger.LogInformation("Firmado {Url}", currentUrl);
                    }
                }
                else
                {
                    firmaOmitidaLastmod++;
                }
            }

            // EMBEDDING (Fase 4). Multilingüe: se embebe el texto ORIGINAL (título + cuerpo), SIN
            // traducir, para cualquier idioma. El texto se usó en memoria (textoPlano) y aquí solo
            // se guarda el VECTOR; el texto NUNCA se persiste. Optimización lastmod idéntica a la firma.
            if (embedHabilitado)
            {
                var lastmodCambio = publishedAt.HasValue && oldPublishedAt != publishedAt;
                var necesitaEmbedding = esNuevo || embeddingExistente == null || lastmodCambio;
                if (necesitaEmbedding)
                {
                    var textoEmbed = ConstruirTextoEmbed(meta.Title, textoPlano);
                    if (string.IsNullOrWhiteSpace(textoEmbed))
                    {
                        // Sin texto embebible: se deja NULL (no es error).
                    }
                    else
                    {
                        var vector = await voyage.EmbedUnoAsync(textoEmbed, stoppingToken);
                        if (vector == null)
                        {
                            embedErrores++;
                            _logger.LogWarning("Embedding vacío/fallido; no se embebe {Url}", currentUrl);
                        }
                        else
                        {
                            anchor.Embedding = JsonSerializer.Serialize(vector);
                            anchor.EmbeddingModelo = voyage.Modelo;
                            anchor.EmbeddingCalculadoEn = DateTime.UtcNow;
                            await db.SaveChangesAsync(stoppingToken);
                            embebidas++;
                            _logger.LogInformation("Embebido {Url}", currentUrl);
                        }
                    }
                }
                else
                {
                    embedOmitidaLastmod++;
                }
            }
        }

        // Procesa UNA URL con el flujo YA VALIDADO: robots + descarga(charset) + filtro meta + persistencia.
        // Devuelve el HTML (para descubrir enlaces en modo BFS) o "" si se saltó/erró. Incrementa contadores.
        async Task<string> ProcesarUrlAsync(string currentUrl, DateTime? lastmod)
        {
            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
                return string.Empty;

            // robots.txt: ¿está permitido acceder a esta URL para nuestro bot?
            var robots = await GetRobotsAsync(currentUri);
            if (!robots.IsAllowed(BotUserAgentProduct, currentUri.PathAndQuery))
            {
                _logger.LogInformation("robots.txt DISALLOW {Url} — se salta", currentUrl);
                robotsSkipped++;
                return string.Empty;
            }

            _logger.LogInformation("Indexando {Url}", currentUrl);

            // Descargar la página EN MEMORIA (con detección de charset). El HTML NO se persiste.
            var (ok, html, error) = await DownloadHtmlAsync(httpClient, currentUrl, stoppingToken);
            if (!ok)
            {
                _logger.LogWarning("Error descargando {Url}: {Error}", currentUrl, error);
                errors++;
                await DelayPoliteAsync(robots, stoppingToken);
                return string.Empty;
            }

            // Extraer SOLO metadatos (nunca el cuerpo del artículo).
            var meta = ExtractMetadata(html, currentUrl, defaultLanguage);

            // FILTRO: solo se indexan páginas con meta-description (contenido real).
            if (string.IsNullOrWhiteSpace(meta.Snippet))
            {
                _logger.LogInformation("Sin meta ni cuerpo útil, no se indexa: {Url}", currentUrl);
                noMeta++;
            }
            else
            {
                // Texto principal para firmar y/o embeber: se extrae del HTML en memoria excluyendo
                // script/style/nav/header/footer/aside; nunca se guarda. Se extrae si la fuente se
                // firma O si hay embeddings (el embedding aplica a cualquier idioma sin traducir).
                var textoPlano = (firmarHabilitado || embedHabilitado) ? ExtraerTextoPrincipal(html) : null;
                await PersistirAsync(currentUrl, meta, lastmod, textoPlano);
                indexed++;
            }

            pagesProcessed++;
            // Pausa entre peticiones (respeta Crawl-delay de robots.txt si existe).
            await DelayPoliteAsync(robots, stoppingToken);
            return html;
        }

        _logger.LogInformation(
            "Fuente '{Nombre}': inicio={Start}, maxDepth={MaxDepth}, maxPages={MaxPages}, idioma={Idioma}, pais={Pais}, rutas={Rutas}",
            fuente.Nombre, fuente.UrlInicial, maxDepth, maxPages, fuente.Idioma, fuente.Pais,
            rutasPermitidas.Count == 0 ? "(todo el host)" : string.Join(",", rutasPermitidas));

        // --- Descubrimiento de URLs: sitemap (si aplica) o BFS de enlaces (fallback) ---
        List<SitemapReader.SitemapUrl>? sitemapUrls = null;
        string motivoBfs = "desconocido";
        if (!fuente.UsarSitemap)
        {
            motivoBfs = "usarSitemap=false";
        }
        else
        {
            try
            {
                var reader = new SitemapReader(httpClient, _logger);
                var sitemapUrl = await reader.DescubrirSitemapAsync(fuente.UrlInicial, fuente.SitemapUrl, stoppingToken);
                if (sitemapUrl == null)
                {
                    motivoBfs = "no se encontró sitemap (JSON/robots/rutas estándar)";
                }
                else
                {
                    _logger.LogInformation("Fuente '{Nombre}': sitemap encontrado en {Sitemap}; leyendo URLs...", fuente.Nombre, sitemapUrl);
                    sitemapUrls = await reader.LeerUrlsAsync(sitemapUrl, fuente.ExcluirSitemaps ?? new List<string>(), stoppingToken);
                    sitemapCount = sitemapUrls.Count;
                    if (sitemapCount == 0) motivoBfs = $"sitemap {sitemapUrl} devolvió 0 URLs";
                }
            }
            catch (Exception ex)
            {
                // DIAGNÓSTICO: no tragar la excepción; dejar claro por qué se cae a BFS.
                _logger.LogError(ex, "ERROR sitemap fuente '{Nombre}' ({Sitemap}): {Msg}",
                    fuente.Nombre, fuente.SitemapUrl ?? "(descubrir)", ex.Message);
                motivoBfs = "excepción leyendo sitemap: " + ex.Message;
            }
        }

        if (sitemapUrls != null && sitemapUrls.Count > 0)
        {
            // MODO SITEMAP: la lista del sitemap reemplaza el BFS. No se descubren enlaces.
            _logger.LogInformation("Fuente '{Nombre}': usando SITEMAP con {N} URLs", fuente.Nombre, sitemapCount);
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in sitemapUrls)
            {
                if (stoppingToken.IsCancellationRequested || pagesProcessed >= maxPages) break;

                if (!Uri.TryCreate(item.Loc, UriKind.Absolute, out var uri)) continue;
                // Respetar hostPermitidos (+ rutasPermitidas si la fuente las define).
                if (!EsUrlPermitida(uri, hostsPermitidos, rutasPermitidas)) continue;

                var normalized = NormalizeUrl(item.Loc);
                if (!vistas.Add(normalized)) continue;

                await ProcesarUrlAsync(normalized, item.LastMod);
            }
        }
        else
        {
            // FALLBACK: BFS de enlaces (comportamiento anterior). La urlInicial debe estar dentro de host + rutas.
            _logger.LogWarning("Fuente '{Nombre}': usando BFS de enlaces (motivo: {Motivo}); inicio={Inicial}",
                fuente.Nombre, motivoBfs, NormalizeUrl(fuente.UrlInicial));
            if (!EsUrlPermitida(startUri, hostsPermitidos, rutasPermitidas))
            {
                _logger.LogWarning("Fuente '{Nombre}': urlInicial fuera de hostPermitidos/rutasPermitidas; no se indexa nada.", fuente.Nombre);
                return;
            }

            var queue = new Queue<(string Url, int Depth)>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedStart = NormalizeUrl(fuente.UrlInicial);
            queue.Enqueue((normalizedStart, 0));
            visited.Add(normalizedStart);

            while (queue.Count > 0 && !stoppingToken.IsCancellationRequested && pagesProcessed < maxPages)
            {
                var (currentUrl, depth) = queue.Dequeue();

                var html = await ProcesarUrlAsync(currentUrl, null);
                if (html.Length == 0) continue; // saltada/errada: no se descubren enlaces

                // Si aún no llegamos a profundidad máxima, extraer nuevos enlaces (también en páginas sin meta).
                if (depth < maxDepth)
                {
                    foreach (var link in HtmlLinkExtractor.ExtractLinks(html, baseUri))
                    {
                        if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                            continue;

                        // Solo host permitido Y (si aplica) dentro de las rutasPermitidas de la fuente.
                        if (!EsUrlPermitida(linkUri, hostsPermitidos, rutasPermitidas))
                            continue;

                        var normalized = NormalizeUrl(linkUri.ToString());

                        if (visited.Contains(normalized))
                            continue;

                        visited.Add(normalized);
                        queue.Enqueue((normalized, depth + 1));
                    }
                }
            }
        }

        _logger.LogInformation(
            "Fuente '{Nombre}': sitemap con {N} URLs, {P} indexadas (con meta), {Q} sin meta, {R} por robots, {E} errores. " +
            "Firma (idioma={Idioma}, habilitada={Hab}): {F} firmadas, {T} traducidas (EN→ES), {L} omitidas por lastmod, {FE} errores de firma/traducción. " +
            "Embedding (Voyage, habilitado={EmbHab}): {EM} embebidas, {EL} omitidas por lastmod, {EE} errores.",
            fuente.Nombre, sitemapCount, indexed, noMeta, robotsSkipped, errors,
            defaultLanguage, firmarHabilitado, firmadas, traducidas, firmaOmitidaLastmod, firmaErrores,
            embedHabilitado, embebidas, embedOmitidaLastmod, embedErrores);
    }

    // Texto para el embedding: título + cuerpo (ya en texto plano por ExtraerTextoPrincipal),
    // colapsado y acotado. SIN traducir ni normalizar: el modelo multilingüe usa el significado
    // natural. Solo EN MEMORIA; nunca se persiste el texto (solo el vector resultante).
    private static readonly System.Text.RegularExpressions.Regex EspaciosEmbedRegex =
        new(@"\s+", System.Text.RegularExpressions.RegexOptions.Compiled);
    private const int MaxCharsEmbed = 32000;

    private static string ConstruirTextoEmbed(string? titulo, string? cuerpo)
    {
        var texto = EspaciosEmbedRegex.Replace($"{titulo}. {cuerpo}", " ").Trim();
        if (texto.Length > MaxCharsEmbed) texto = texto.Substring(0, MaxCharsEmbed);
        return texto;
    }

    // Extrae el texto principal para firmar: quita script/style/nav/header/footer/aside
    // (ruido no editorial que contaminaría el conteo) y devuelve el texto del body.
    // Solo para el cálculo de la firma EN MEMORIA; nunca se persiste.
    private static string ExtraerTextoPrincipal(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var root = doc.DocumentNode;

        var descartar = root.SelectNodes("//script|//style|//nav|//header|//footer|//aside");
        if (descartar != null)
            foreach (var n in descartar) n.Remove();

        var body = root.SelectSingleNode("//body") ?? root;
        return HtmlEntity.DeEntitize(body.InnerText ?? string.Empty);
    }

    // ¿El host de la URL está en la allow-list de la fuente? (el JSON lista www y no-www; el strip es red de seguridad)
    private static bool EsHostPermitido(Uri uri, HashSet<string> permitidos)
    {
        if (permitidos.Contains(uri.Host)) return true;
        var sinWww = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host.Substring(4) : uri.Host;
        return permitidos.Contains(sinWww);
    }

    // ¿La URL pertenece a la fuente? Host permitido Y (si hay rutasPermitidas) el path empieza
    // con alguno de esos prefijos (case-insensitive). Sin rutasPermitidas = todo el host.
    private static bool EsUrlPermitida(Uri uri, HashSet<string> hostsPermitidos, IReadOnlyList<string> rutasPermitidas)
    {
        if (!EsHostPermitido(uri, hostsPermitidos)) return false;
        if (rutasPermitidas.Count == 0) return true;
        foreach (var prefijo in rutasPermitidas)
        {
            if (string.IsNullOrEmpty(prefijo)) continue;
            if (uri.AbsolutePath.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // Des-escapar &amp; -> &
        url = url.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        // Forzar https (los sitios objetivo usan https)
        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            uri = new UriBuilder(uri)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = -1 // puerto por defecto
            }.Uri;
        }

        // Quitar fragmento (#...)
        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        var normalized = builder.Uri.ToString().TrimEnd('/');
        return normalized;
    }

    // Descarga la página en memoria detectando el charset (header Content-Type -> <meta charset> -> UTF-8).
    // Devuelve el HTML como string; el llamador lo usa para metadatos/enlaces y luego lo descarta.
    private static async Task<(bool Ok, string Html, string? Error)> DownloadHtmlAsync(
        HttpClient httpClient, string url, CancellationToken ct)
    {
        try
        {
            using var resp = await httpClient.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
                return (false, string.Empty, $"HTTP {(int)resp.StatusCode}");

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

            var enc = Encoding.UTF8;
            if (!TrySetEncoding(resp.Content.Headers.ContentType?.CharSet, ref enc))
            {
                // Sniff del <meta charset=...> en los primeros bytes (ASCII).
                var probe = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
                var m = Regex.Match(probe, "charset\\s*=\\s*[\"']?([\\w-]+)", RegexOptions.IgnoreCase);
                if (m.Success) TrySetEncoding(m.Groups[1].Value, ref enc);
            }

            return (true, enc.GetString(bytes), null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, ex.Message);
        }
    }

    private static bool TrySetEncoding(string? name, ref Encoding enc)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        try { enc = Encoding.GetEncoding(name.Trim().Trim('"', '\'')); return true; }
        catch { return false; }
    }

    // Recorta un texto a un snippet corto (máx 160 chars ESTRICTO), cortando en límite de palabra
    // y agregando "…" si se truncó. Colapsa espacios. Es un fragmento descriptivo, jamás el cuerpo.
    private static string RecortarSnippet(string text, int max = 160)
    {
        text = Regex.Replace(text, "\\s+", " ").Trim();
        if (text.Length <= max) return text;
        var limite = max - 1; // reservar 1 char para el "…"
        var cut = text.Substring(0, limite);
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > 0) cut = cut.Substring(0, lastSpace);
        return cut.TrimEnd() + "…";
    }

    // Extrae SOLO metadatos + un snippet corto (meta-description o, si no hay, el primer párrafo
    // real del cuerpo, máx 160 chars). NUNCA devuelve el cuerpo del artículo.
    private static (string Title, string Snippet, string Language, string? MainTopic, string SnippetSource) ExtractMetadata(
        string html, string currentUrl, string defaultLanguage)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var root = doc.DocumentNode;

        string? Meta(string attr, string name)
        {
            var node = root.SelectSingleNode(
                $"//meta[translate(@{attr},'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='{name}']");
            var c = node?.GetAttributeValue("content", string.Empty);
            return string.IsNullOrWhiteSpace(c) ? null : HtmlEntity.DeEntitize(c).Trim();
        }

        // Título: <title> | og:title | <h1> | (último recurso) la URL, para no violar NOT NULL.
        var title = root.SelectSingleNode("//title")?.InnerText;
        if (string.IsNullOrWhiteSpace(title)) title = Meta("property", "og:title");
        if (string.IsNullOrWhiteSpace(title)) title = root.SelectSingleNode("//h1")?.InnerText;
        title = string.IsNullOrWhiteSpace(title) ? currentUrl : HtmlEntity.DeEntitize(title).Trim();
        if (title.Length > 500) title = title.Substring(0, 500);

        // Snippet: meta-description / og:description; si no hay, el primer párrafo real del cuerpo.
        // Siempre limitado a 160 chars (RecortarSnippet); jamás se guarda más que ese fragmento.
        string? PrimerParrafoSustancial()
        {
            var ps = root.SelectNodes(
                "//p[not(ancestor::nav) and not(ancestor::header) and not(ancestor::footer) and not(ancestor::aside)]");
            if (ps == null) return null;
            foreach (var p in ps)
            {
                var txt = HtmlEntity.DeEntitize(p.InnerText ?? string.Empty);
                txt = Regex.Replace(txt, "\\s+", " ").Trim();
                if (txt.Length >= 40) return txt; // texto sustancial (ignora menús/labels/vacíos)
            }
            return null;
        }

        string snippet;
        string snippetSource;
        var metaDesc = Meta("name", "description") ?? Meta("property", "og:description");
        if (!string.IsNullOrWhiteSpace(metaDesc))
        {
            snippet = RecortarSnippet(metaDesc);
            snippetSource = "meta-description";
        }
        else
        {
            var parrafo = PrimerParrafoSustancial();
            if (!string.IsNullOrWhiteSpace(parrafo))
            {
                snippet = RecortarSnippet(parrafo);
                snippetSource = "cuerpo";
            }
            else
            {
                snippet = string.Empty;      // ni meta ni cuerpo útil -> no se indexará
                snippetSource = string.Empty;
            }
        }

        // Idioma: <html lang> | og:locale | default de la fuente.
        var lang = root.SelectSingleNode("//html")?.GetAttributeValue("lang", string.Empty);
        if (string.IsNullOrWhiteSpace(lang)) lang = Meta("property", "og:locale");
        if (string.IsNullOrWhiteSpace(lang)) lang = defaultLanguage;
        lang = lang.Trim();
        if (lang.Length > 10) lang = lang.Substring(0, 10);

        // MainTopic: og:type si existe; si no, null.
        var mainTopic = Meta("property", "og:type");
        if (mainTopic != null && mainTopic.Length > 200) mainTopic = mainTopic.Substring(0, 200);

        return (title, snippet, lang, mainTopic, snippetSource);
    }
}
