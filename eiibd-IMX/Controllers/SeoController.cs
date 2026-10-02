using System.Text;
using System.Xml;
using eiibd_IMX.Models.ViewModels;
using eiibd_IMX.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace eiibd_IMX.Controllers;

/// robots.txt y sitemap.xml. Los dos se SIRVEN POR RUTA, no como archivos
/// estaticos en wwwroot, para que el host salga de Sitio:HostCanonico y no haya
/// forma de que un archivo suelto quede apuntando a un dominio viejo.
///
/// Sus rutas llevan segmento literal y se registran antes de los catch-all
/// (ver Program.cs); si no, "robots.txt" entraria como slug de categoria.
public sealed class SeoController : Controller
{
    private const string ClaveCacheSitemap = "sitemap-xml";
    private static readonly TimeSpan VidaCache = TimeSpan.FromMinutes(30);

    private readonly IContenidoService _contenido;
    private readonly IConfiguration _cfg;
    private readonly IMemoryCache _cache;

    public SeoController(IContenidoService contenido, IConfiguration cfg, IMemoryCache cache)
    {
        _contenido = contenido;
        _cfg = cfg;
        _cache = cache;
    }

    private string Host => (_cfg["Sitio:HostCanonico"] ?? "").TrimEnd('/');

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var txt = $"User-agent: *\nAllow: /\nSitemap: {Host}/sitemap.xml\n";
        return Content(txt, "text/plain; charset=utf-8");
    }

    [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        // El sitemap recorre TODAS las categorias y todos sus articulos: es la
        // consulta mas cara del sitio y la piden bots, no personas. Cacheado
        // 30 min, un rastreo insistente no toca la base mas de dos veces por hora.
        if (!_cache.TryGetValue(ClaveCacheSitemap, out string? xml) || xml is null)
        {
            xml = await ConstruirSitemapAsync(ct);
            _cache.Set(ClaveCacheSitemap, xml, VidaCache);
        }
        return Content(xml, "application/xml; charset=utf-8");
    }

    private async Task<string> ConstruirSitemapAsync(CancellationToken ct)
    {
        var slugs = _cfg.GetSection("Filas").GetChildren()
            .Select(f => f["CategoriaSlug"] ?? "")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entradas = new List<UrlSitemapVm>
        {
            new() { Ruta = "/",                     Prioridad = "1.0", Frecuencia = "weekly" },
            new() { Ruta = "/quienes-somos",        Prioridad = "0.5", Frecuencia = "yearly" },
            new() { Ruta = "/transparencia",        Prioridad = "0.5", Frecuencia = "yearly" },
            new() { Ruta = "/contacto",             Prioridad = "0.5", Frecuencia = "yearly" },
            // El aviso de privacidad entra al sitemap a proposito: es indexable
            // y un revisor de Ad Grants espera poder encontrarlo.
            new() { Ruta = "/aviso-de-privacidad",  Prioridad = "0.3", Frecuencia = "yearly" }
        };

        foreach (var slug in slugs)
        {
            var articulos = await _contenido.ObtenerUrlsDeCategoriaAsync(slug, ct);

            // La categoria se fecha con su articulo mas reciente: es lo que
            // realmente cambia en esa pagina. Sin articulos publicados la
            // categoria no entra: seria una URL de listado vacio.
            if (articulos.Count == 0) continue;

            entradas.Add(new UrlSitemapVm
            {
                Ruta       = $"/{slug}",
                Lastmod    = articulos.Max(a => a.Lastmod),
                Prioridad  = "0.8",
                Frecuencia = "weekly"
            });
            entradas.AddRange(articulos);
        }

        // Un articulo puede colgar de dos categorias y salir con dos rutas
        // distintas. Son URLs distintas y validas, pero una repetida no: dedup.
        entradas = entradas
            .GroupBy(e => e.Ruta, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var sb = new StringBuilder();
        var opciones = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            OmitXmlDeclaration = false
        };

        using (var w = XmlWriter.Create(sb, opciones))
        {
            w.WriteStartDocument();
            w.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var e in entradas)
            {
                w.WriteStartElement("url");
                w.WriteElementString("loc", Host + e.Ruta);
                if (e.Lastmod is not null)
                    w.WriteElementString("lastmod", e.Lastmod.Value.ToString("yyyy-MM-dd"));
                w.WriteElementString("changefreq", e.Frecuencia);
                w.WriteElementString("priority", e.Prioridad);
                w.WriteEndElement();
            }
            w.WriteEndElement();
            w.WriteEndDocument();
        }

        // XmlWriter con StringBuilder escribe la declaracion con encoding="utf-16"
        // (el StringBuilder ES UTF-16), pero la respuesta sale en UTF-8. Sin este
        // ajuste un parser estricto rechaza el archivo entero.
        return sb.ToString().Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"");
    }
}
