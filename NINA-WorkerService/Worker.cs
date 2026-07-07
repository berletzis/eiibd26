using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

        // Fuentes desde fuentes.json (editable a mano, viaja junto al ejecutable).
        var fuentes = CargarFuentes();
        var activas = fuentes.Where(f => f.Activo).ToList();
        _logger.LogInformation("Fuentes activas: {Count} de {Total}", activas.Count, fuentes.Count);

        // Un solo HttpClient (UA honesto) para todas las fuentes.
        using var httpClient = CrearHttpClient();

        // robots.txt: caché por host, compartida entre fuentes (se lee robots una sola vez por host).
        var robotsCache = new Dictionary<string, RobotsMatcher>(StringComparer.OrdinalIgnoreCase);

        foreach (var fuente in activas)
        {
            if (stoppingToken.IsCancellationRequested) break;

            var site = await UpsertSourceSiteAsync(db, fuente, stoppingToken);
            await IndexarFuenteAsync(db, httpClient, robotsCache, site, fuente, stoppingToken);
        }

        _logger.LogInformation("ScrapingWorker finalizado (ejecución única).");
    }

    // Carga y deserializa fuentes.json (System.Text.Json, case-insensitive; modo como enum de texto).
    private List<FuenteConfig> CargarFuentes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fuentes.json");
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        return JsonSerializer.Deserialize<List<FuenteConfig>>(json, options) ?? new List<FuenteConfig>();
    }

    private static HttpClient CrearHttpClient()
    {
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        if (!httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(BotUserAgentFull))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", BotUserAgentFull);
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

    // Indexa UNA fuente: BFS + robots + charset + filtro meta-description + metadatos -> Article (ancla ScrapedPage).
    // Lógica ya validada; solo parametrizada por la fuente (allow-list, url inicial, límites, idioma).
    private async Task IndexarFuenteAsync(
        Eiibd26Context db, HttpClient httpClient, Dictionary<string, RobotsMatcher> robotsCache,
        SourceSite site, FuenteConfig fuente, CancellationToken stoppingToken)
    {
        var maxDepth = fuente.MaxDepth > 0 ? fuente.MaxDepth : 10;
        var maxPages = fuente.MaxPages > 0 ? fuente.MaxPages : 3000;
        var defaultLanguage = string.IsNullOrWhiteSpace(fuente.Idioma) ? "es" : fuente.Idioma!;
        var hostsPermitidos = new HashSet<string>(fuente.HostPermitidos, StringComparer.OrdinalIgnoreCase);

        var startUri = new Uri(fuente.UrlInicial);
        var baseUri = new Uri($"{startUri.Scheme}://{startUri.Host}/");

        _logger.LogInformation(
            "Fuente '{Nombre}': inicio={Start}, maxDepth={MaxDepth}, maxPages={MaxPages}, idioma={Idioma}, pais={Pais}",
            fuente.Nombre, fuente.UrlInicial, maxDepth, maxPages, fuente.Idioma, fuente.Pais);

        // Cola de URLs (con profundidad) y visitadas en esta ejecución de la fuente.
        var queue = new Queue<(string Url, int Depth)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedStart = NormalizeUrl(fuente.UrlInicial);
        queue.Enqueue((normalizedStart, 0));
        visited.Add(normalizedStart);

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

        var pagesProcessed = 0;
        var robotsSkipped = 0;
        var indexed = 0;
        var noMeta = 0;
        var errors = 0;

        while (queue.Count > 0 && !stoppingToken.IsCancellationRequested && pagesProcessed < maxPages)
        {
            var (currentUrl, depth) = queue.Dequeue();

            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri))
                continue;

            // robots.txt: ¿está permitido acceder a esta URL para nuestro bot?
            var robots = await GetRobotsAsync(currentUri);
            if (!robots.IsAllowed(BotUserAgentProduct, currentUri.PathAndQuery))
            {
                _logger.LogInformation("robots.txt DISALLOW {Url} — se salta", currentUrl);
                robotsSkipped++;
                continue;
            }

            _logger.LogInformation("Indexando {Url} (nivel {Depth})", currentUrl, depth);

            // Descargar la página EN MEMORIA (con detección de charset). El HTML NO se persiste.
            var (ok, html, error) = await DownloadHtmlAsync(httpClient, currentUrl, stoppingToken);
            if (!ok)
            {
                _logger.LogWarning("Error descargando {Url}: {Error}", currentUrl, error);
                errors++;
                await DelayPoliteAsync(robots, stoppingToken);
                continue;
            }

            // Extraer SOLO metadatos (nunca el cuerpo del artículo).
            var meta = ExtractMetadata(html, currentUrl, defaultLanguage);

            // FILTRO: solo se indexan páginas con meta-description (contenido real).
            // Las páginas sin meta (secciones/listados) NO se indexan, pero SÍ se visitan
            // para seguir descubriendo enlaces (el BFS continúa; ver extracción de enlaces abajo).
            if (string.IsNullOrWhiteSpace(meta.Snippet))
            {
                _logger.LogInformation("Sin meta-description, no se indexa: {Url}", currentUrl);
                noMeta++;
            }
            else
            {
                // Persistencia estilo índice (Opción A):
                //  - ScrapedPage = ANCLA de URL: solo Url + SourceSiteId; ContentRaw = "" (jamás el HTML).
                //  - Article = metadatos, colgado del ancla por ScrapedPageId; hereda la fuente vía el ancla.
                var anchor = await db.ScrapedPages
                    .FirstOrDefaultAsync(p => p.Url == currentUrl && p.SourceSiteId == site.SourceSiteId, stoppingToken);

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

                _logger.LogInformation("Indexado {Url} -> ArticleId {Id}", currentUrl, article.ArticleId);
                indexed++;
            }

            pagesProcessed++;

            // Si aún no llegamos a profundidad máxima, extraer nuevos enlaces (también en páginas sin meta).
            if (depth < maxDepth)
            {
                foreach (var link in HtmlLinkExtractor.ExtractLinks(html, baseUri))
                {
                    if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                        continue;

                    // Solo hosts permitidos de ESTA fuente (allow-list del JSON).
                    if (!EsHostPermitido(linkUri, hostsPermitidos))
                        continue;

                    var normalized = NormalizeUrl(linkUri.ToString());

                    if (visited.Contains(normalized))
                        continue;

                    visited.Add(normalized);
                    queue.Enqueue((normalized, depth + 1));
                }
            }

            // Pausa entre peticiones (respeta Crawl-delay de robots.txt si existe).
            await DelayPoliteAsync(robots, stoppingToken);
        }

        _logger.LogInformation(
            "Fuente '{Nombre}': {Indexed} indexadas (con meta), {NoMeta} sin meta saltadas, {Robots} por robots, {Errors} errores",
            fuente.Nombre, indexed, noMeta, robotsSkipped, errors);
    }

    // ¿El host de la URL está en la allow-list de la fuente? (el JSON lista www y no-www; el strip es red de seguridad)
    private static bool EsHostPermitido(Uri uri, HashSet<string> permitidos)
    {
        if (permitidos.Contains(uri.Host)) return true;
        var sinWww = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host.Substring(4) : uri.Host;
        return permitidos.Contains(sinWww);
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

    // Extrae SOLO metadatos de la página. NUNCA devuelve el cuerpo del artículo.
    private static (string Title, string Snippet, string Language, string? MainTopic) ExtractMetadata(
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

        // Snippet: SOLO meta description / og:description. Si no hay, "" (jamás el cuerpo del artículo).
        var snippet = Meta("name", "description") ?? Meta("property", "og:description") ?? string.Empty;

        // Idioma: <html lang> | og:locale | default de la fuente.
        var lang = root.SelectSingleNode("//html")?.GetAttributeValue("lang", string.Empty);
        if (string.IsNullOrWhiteSpace(lang)) lang = Meta("property", "og:locale");
        if (string.IsNullOrWhiteSpace(lang)) lang = defaultLanguage;
        lang = lang.Trim();
        if (lang.Length > 10) lang = lang.Substring(0, 10);

        // MainTopic: og:type si existe; si no, null.
        var mainTopic = Meta("property", "og:type");
        if (mainTopic != null && mainTopic.Length > 200) mainTopic = mainTopic.Substring(0, 200);

        return (title, snippet, lang, mainTopic);
    }
}
