using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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

    // Lista blanca de hosts permitidos
    private static readonly string[] AllowedHosts = new[]
    {
        "funeiicocom/"   // TODO: añade más dominios si quieres
    };

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

        // 1. Configuración del sitio y profundidad
        var baseUrl = "https://funeiico.com/";   // TODO: dominio base
        var startUrl = "https://funeiico.com/"; // TODO: URL de inicio
        var maxDepth = 10;                          // profundidad máxima (0=solo home)
        var maxPages = 3000;                        // límite de páginas por ejecución

        try
        {
            // 2. Asegurar SourceSite
            var site = await db.SourceSites
                .FirstOrDefaultAsync(s => s.BaseUrl == baseUrl, stoppingToken);

            if (site == null)
            {
                site = new SourceSite
                {
                    Name = "crohns colitis foundation", // nombre amigable
                    BaseUrl = baseUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.SourceSites.Add(site);
                await db.SaveChangesAsync(stoppingToken);
            }

            var baseUri = new Uri(baseUrl);

            // 3. Cola de URLs (con profundidad) y conjunto de visitadas en esta ejecución
            var queue = new Queue<(string Url, int Depth)>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var normalizedStart = NormalizeUrl(startUrl);
            queue.Enqueue((normalizedStart, 0));
            visited.Add(normalizedStart);

            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // User-Agent “de navegador”
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Mozilla", "5.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Chrome", "123.0.0.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Safari", "537.36"));

            var pagesProcessed = 0;

            while (queue.Count > 0 && !stoppingToken.IsCancellationRequested && pagesProcessed < maxPages)
            {
                var (currentUrl, depth) = queue.Dequeue();

                _logger.LogInformation("Scrapeando {Url} (nivel {Depth})", currentUrl, depth);

                // 4. Verificar si ya tenemos última versión de esta URL en BD
                var lastPage = await db.ScrapedPages
                    .Where(p => p.Url == currentUrl)
                    .OrderByDescending(p => p.ScrapedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                string html;

                try
                {
                    _logger.LogDebug("Descargando: {Url}", currentUrl);
                    html = await httpClient.GetStringAsync(currentUrl, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error descargando {Url}", currentUrl);

                    // Guardar el fallo en ScrapedPage
                    var errorPage = new ScrapedPage
                    {
                        SourceSiteId = site.SourceSiteId,
                        Url = currentUrl,
                        TitleRaw = null,
                        ContentRaw = string.Empty,
                        ContentText = null,
                        Language = "es",
                        ScrapedAt = DateTime.UtcNow,
                        Status = "ERROR",
                        ErrorMessage = ex.Message
                    };

                    db.ScrapedPages.Add(errorPage);
                    await db.SaveChangesAsync(stoppingToken);

                    // Pequeña pausa para no ser agresivos
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                    continue;
                }

                // 5. Calcular hash del contenido
                var hash = ComputeSha256(html);

                // Si ya teníamos una versión y el hash es igual → no insertamos nueva fila
                if (lastPage != null && lastPage.HashContent != null &&
                    lastPage.HashContent.SequenceEqual(hash))
                {
                    _logger.LogInformation("Sin cambios en {Url}. Actualizando solo ScrapedAt.", currentUrl);

                    lastPage.ScrapedAt = DateTime.UtcNow;
                    lastPage.Status = "OK";
                    lastPage.ErrorMessage = null;

                    await db.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    // Contenido nuevo o URL nueva → guardamos una nueva versión
                    var scrapedPage = new ScrapedPage
                    {
                        SourceSiteId = site.SourceSiteId,
                        Url = currentUrl,
                        TitleRaw = null,      // luego extraeremos <title>
                        ContentRaw = html,
                        ContentText = null,   // luego lo limpiaremos
                        Language = "es",      // más adelante puedes detectar idioma real
                        ScrapedAt = DateTime.UtcNow,
                        Status = "OK",
                        HashContent = hash
                    };

                    db.ScrapedPages.Add(scrapedPage);
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Guardada nueva versión de {Url} con ScrapedPageId {Id}",
                        currentUrl, scrapedPage.ScrapedPageId);
                }

                pagesProcessed++;

                // 6. Si aún no llegamos a profundidad máxima, extraer nuevos enlaces
                if (depth < maxDepth)
                {
                    foreach (var link in HtmlLinkExtractor.ExtractLinks(html, baseUri))
                    {
                        if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                            continue;

                        // Solo hosts permitidos
                        if (!IsAllowedHost(linkUri))
                            continue;

                        var normalized = NormalizeUrl(linkUri.ToString());

                        // Evitar duplicados en esta ejecución
                        if (visited.Contains(normalized))
                            continue;

                        visited.Add(normalized);
                        queue.Enqueue((normalized, depth + 1));

                        _logger.LogDebug("Encolada URL: {Url} (nivel {Depth})", normalized, depth + 1);
                    }
                }

                // Pausa entre peticiones para ser amables con el servidor
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }

            _logger.LogInformation("Scraping terminado. Páginas procesadas: {Count}", pagesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general en ScrapingWorker");
        }

        _logger.LogInformation("ScrapingWorker finalizado (ejecución única).");
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        // Des-escapar &amp; -> &
        url = url.Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        // Forzar https para este sitio (opcional, pero cucicrohncr usa https)
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

    private static bool IsAllowedHost(Uri uri)
    {
        var host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        return AllowedHosts.Contains(host, StringComparer.OrdinalIgnoreCase);
    }

    private static byte[] ComputeSha256(string text)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        return sha256.ComputeHash(bytes);
    }
}