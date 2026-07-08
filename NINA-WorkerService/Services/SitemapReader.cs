using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace NINA_WorkerService.Services;

/// <summary>
/// Lee sitemaps XML (estándar sitemaps.org) como fuente de URLs. Sin dependencias nuevas
/// (System.Xml.Linq). Soporta los dos tipos del estándar:
///   - &lt;sitemapindex&gt;: índice con &lt;sitemap&gt;&lt;loc&gt; a sub-sitemaps (un nivel de recursión).
///   - &lt;urlset&gt;: lista de &lt;url&gt;&lt;loc&gt; (+ &lt;lastmod&gt; opcional).
/// El parseo es agnóstico al namespace (compara por LocalName), porque los sitemaps declaran
/// xmlns="http://www.sitemaps.org/schemas/sitemap/0.9".
/// </summary>
public sealed class SitemapReader
{
    public readonly record struct SitemapUrl(string Loc, DateTime? LastMod);

    private readonly HttpClient _http;
    private readonly ILogger _logger;

    public SitemapReader(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Descubre la URL del sitemap, en orden: (1) la del JSON; (2) línea "Sitemap:" del robots.txt;
    /// (3) fallback a /sitemap_index.xml y /sitemap.xml. Devuelve null si no encuentra ninguno.
    /// </summary>
    public async Task<string?> DescubrirSitemapAsync(string urlInicial, string? sitemapUrlJson, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sitemapUrlJson))
            return sitemapUrlJson.Trim();

        if (!Uri.TryCreate(urlInicial, UriKind.Absolute, out var u))
            return null;

        var root = $"{u.Scheme}://{u.Host}";

        // (2) Sitemap: en robots.txt
        var robotsTxt = await DescargarTextoAsync($"{root}/robots.txt", ct);
        if (robotsTxt != null)
        {
            foreach (var line in robotsTxt.Replace("\r", "").Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                {
                    var loc = t.Substring("Sitemap:".Length).Trim();
                    if (!string.IsNullOrEmpty(loc))
                    {
                        _logger.LogInformation("Sitemap desde robots.txt: {Loc}", loc);
                        return loc;
                    }
                }
            }
        }

        // (3) fallback a rutas estándar
        foreach (var cand in new[] { $"{root}/sitemap_index.xml", $"{root}/sitemap.xml" })
        {
            var xml = await DescargarTextoAsync(cand, ct);
            if (xml != null && xml.Contains('<'))
            {
                _logger.LogInformation("Sitemap por fallback: {Loc}", cand);
                return cand;
            }
        }

        return null;
    }

    /// <summary>
    /// Lee todas las URLs finales (&lt;loc&gt; de tipo urlset) de un sitemap. Si es índice,
    /// baja un nivel a cada sub-sitemap (excepto los excluidos por nombre) y junta sus URLs.
    /// </summary>
    public async Task<List<SitemapUrl>> LeerUrlsAsync(string sitemapUrl, IReadOnlyList<string> excluir, CancellationToken ct)
    {
        var result = new List<SitemapUrl>();

        var xml = await DescargarTextoAsync(sitemapUrl, ct);
        if (xml == null)
        {
            _logger.LogWarning("No se pudo descargar el sitemap {Url}", sitemapUrl);
            return result;
        }

        var (subs, urls) = Interpretar(xml);

        if (subs.Count > 0)
        {
            _logger.LogInformation("Sitemap índice con {N} sub-sitemaps", subs.Count);
            foreach (var sub in subs)
            {
                if (excluir.Any(x => sub.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Sub-sitemap excluido: {Sub}", sub);
                    continue;
                }

                var subXml = await DescargarTextoAsync(sub, ct);
                if (subXml == null)
                {
                    _logger.LogWarning("No se pudo descargar sub-sitemap {Sub}", sub);
                    continue;
                }

                var (subSubs, subUrls) = Interpretar(subXml);
                if (subSubs.Count > 0)
                    _logger.LogInformation("Sub-sitemap {Sub} es índice anidado; no se baja más (un nivel).", sub);
                result.AddRange(subUrls);
            }
        }
        else
        {
            result.AddRange(urls);
        }

        return result;
    }

    // Interpreta un XML de sitemap: devuelve sub-sitemaps (si es índice) o URLs (si es urlset).
    private static (List<string> Subs, List<SitemapUrl> Urls) Interpretar(string xml)
    {
        var subs = new List<string>();
        var urls = new List<SitemapUrl>();

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch { return (subs, urls); }

        var root = doc.Root;
        if (root == null) return (subs, urls);

        switch (root.Name.LocalName)
        {
            case "sitemapindex":
                foreach (var sm in root.Elements().Where(e => e.Name.LocalName == "sitemap"))
                {
                    var loc = sm.Elements().FirstOrDefault(e => e.Name.LocalName == "loc")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(loc)) subs.Add(loc);
                }
                break;

            case "urlset":
                foreach (var url in root.Elements().Where(e => e.Name.LocalName == "url"))
                {
                    var loc = url.Elements().FirstOrDefault(e => e.Name.LocalName == "loc")?.Value?.Trim();
                    if (string.IsNullOrEmpty(loc)) continue;

                    var lm = url.Elements().FirstOrDefault(e => e.Name.LocalName == "lastmod")?.Value?.Trim();
                    DateTime? lastmod = null;
                    if (!string.IsNullOrEmpty(lm) && DateTime.TryParse(
                            lm, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
                        lastmod = dt;

                    urls.Add(new SitemapUrl(loc, lastmod));
                }
                break;
        }

        return (subs, urls);
    }

    private async Task<string?> DescargarTextoAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error descargando {Url}", url);
            return null;
        }
    }
}
