using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using eiibd26.Data;

namespace eiibd26.Controllers
{
    // Sitemap específico para la sección "Contenidos"
    // Rutas:
    //  - /sitemap-contenidos.xml        -> índice o único sitemap de contenidos
    //  - /sitemap-contenidos-{n}.xml    -> páginas (1-based)
    //  - /sitemap/refresh               -> invalida caché (solo Administrador)
    [Route("")]
    [AllowAnonymous]
    public class SitemapContenidosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapContenidosController> _logger;

        private const int MaxUrlsPerSitemap = 45000;
        private const string CacheKeyIndex   = "sitemap_contenidos_index_xml";
        private const string CacheKeyPagePfx = "sitemap_contenidos_page_xml_";

        public SitemapContenidosController(
            ApplicationDbContext db,
            IMemoryCache cache,
            ILogger<SitemapContenidosController> logger)
        {
            _db     = db;
            _cache  = cache;
            _logger = logger;
        }

        // GET /sitemap-contenidos.xml
        [HttpGet("sitemap-contenidos.xml")]
        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<string>(CacheKeyIndex, out var cached))
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");

            var total = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado
                         && c.EstadoPublicacion != null
                         && c.EstadoPublicacion != 0
                         && c.ContenidoTituloSlug != null
                         && c.ContenidoTituloSlug != "")
                .CountAsync();

            var xml = total <= MaxUrlsPerSitemap
                ? await GeneratePageXml(0)
                : GenerateIndexXml(total);

            _cache.Set(CacheKeyIndex, xml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        // GET /sitemap-contenidos-{page}.xml  (page es 1-based)
        [HttpGet("sitemap-contenidos-{page}.xml")]
        public async Task<IActionResult> Page(int page)
        {
            if (page < 1) return NotFound();

            var key = CacheKeyPagePfx + page;
            if (_cache.TryGetValue<string>(key, out var cached))
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");

            var xml = await GeneratePageXml(page - 1);
            _cache.Set(key, xml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        // POST /sitemap/refresh  — invalida caché desde el panel admin
        [HttpPost("sitemap/refresh")]
        [Authorize(Roles = "Administrador")]
        public IActionResult Refresh()
        {
            try
            {
                _cache.Remove(CacheKeyIndex);
                for (int i = 1; i <= 100; i++) _cache.Remove(CacheKeyPagePfx + i);
                _logger.LogInformation(
                    "SitemapContenidos cache invalidated by {User}", User?.Identity?.Name ?? "unknown");
                return Json(new { refreshed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating sitemap contenidos cache");
                return StatusCode(500, new { refreshed = false, error = ex.Message });
            }
        }

        private async Task<string> GeneratePageXml(int pageIndex)
        {
            var hostBase = GetHostBase();

            var contenidos = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado
                         && c.EstadoPublicacion != null
                         && c.EstadoPublicacion != 0
                         && c.ContenidoTituloSlug != null
                         && c.ContenidoTituloSlug != "")
                .OrderByDescending(c => c.FechaModificado ?? c.FechaCreado)
                .Skip(pageIndex * MaxUrlsPerSitemap)
                .Take(MaxUrlsPerSitemap)
                .Select(c => new
                {
                    c.ContenidoTituloSlug,
                    LastMod = c.FechaModificado ?? c.FechaCreado
                })
                .ToListAsync();

            var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<UrlEntry>();

            if (pageIndex == 0)
                entries.Add(new UrlEntry
                {
                    Loc        = $"{hostBase}/Contenidos",
                    LastMod    = DateTimeOffset.UtcNow,
                    ChangeFreq = "daily",
                    Priority   = "0.8"
                });

            foreach (var c in contenidos)
            {
                var loc = $"{hostBase}/contenido/{Uri.EscapeDataString(c.ContenidoTituloSlug!)}";
                if (!seen.Add(loc)) continue;
                entries.Add(new UrlEntry
                {
                    Loc        = loc,
                    LastMod    = c.LastMod,
                    ChangeFreq = "weekly",
                    Priority   = "0.6"
                });
            }

            return BuildXml(entries);
        }

        private string GenerateIndexXml(int totalUrls)
        {
            var pages    = (int)Math.Ceiling(totalUrls / (double)MaxUrlsPerSitemap);
            var hostBase = GetHostBase();
            var sb       = new StringBuilder();

            using var xw = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent             = true,
                Encoding           = Encoding.UTF8,
                OmitXmlDeclaration = false
            });

            xw.WriteStartDocument();
            xw.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

            for (int i = 1; i <= pages; i++)
            {
                xw.WriteStartElement("sitemap");
                xw.WriteElementString("loc",     $"{hostBase}/sitemap-contenidos-{i}.xml");
                xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            xw.WriteEndDocument();
            return sb.ToString();
        }

        private string BuildXml(List<UrlEntry> entries)
        {
            var sb = new StringBuilder();

            using var xw = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent             = true,
                Encoding           = Encoding.UTF8,
                OmitXmlDeclaration = false
            });

            xw.WriteStartDocument();
            xw.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var e in entries)
            {
                xw.WriteStartElement("url");
                xw.WriteElementString("loc", e.Loc);
                if (e.LastMod.HasValue)
                    xw.WriteElementString("lastmod",
                        e.LastMod.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                if (!string.IsNullOrWhiteSpace(e.ChangeFreq))
                    xw.WriteElementString("changefreq", e.ChangeFreq);
                if (!string.IsNullOrWhiteSpace(e.Priority))
                    xw.WriteElementString("priority", e.Priority);
                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            xw.WriteEndDocument();

            var xml = sb.ToString();
            xml = xml.Replace(((char)0x00A0).ToString(), " ");
            if (!xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xml;
            else
                xml = System.Text.RegularExpressions.Regex.Replace(
                    xml, "encoding=\".*?\"", "encoding=\"UTF-8\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return xml;
        }

        private string GetHostBase()
        {
            var proto  = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            var scheme = !string.IsNullOrWhiteSpace(proto) ? proto : Request.Scheme;
            if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
                scheme = "https";
            var host = Request.Host.HasValue ? Request.Host.Value : "eiibd.com";
            return $"{scheme}://{host}";
        }

        private sealed class UrlEntry
        {
            public string          Loc        { get; set; } = "";
            public DateTimeOffset? LastMod    { get; set; }
            public string?         ChangeFreq { get; set; }
            public string?         Priority   { get; set; }
        }
    }
}
