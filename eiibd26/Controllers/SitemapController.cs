using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace eiibd26.Controllers
{
    [Route("")]
    [AllowAnonymous]
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapController> _logger;

        private const int MaxUrlsPerSitemap = 45000;
        private const string CacheKeyIndex = "sitemap_index_xml";
        private const string CacheKeyPagePrefix = "sitemap_page_xml_";
        private const string CacheKeyMasterIndex = "sitemap_master_index_xml";

        public SitemapController(ApplicationDbContext db, IMemoryCache cache, ILogger<SitemapController> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        // GET /sitemap.xml
        [HttpGet("sitemap.xml")]
        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<string>(CacheKeyIndex, out var cachedIndex))
                return File(Encoding.UTF8.GetBytes(cachedIndex), "application/xml; charset=utf-8");

            // contar urls estimadas: SOLO contenidos publicados y no eliminados (+ la página principal)
            var contentsCount = await _db.Contenidos.AsNoTracking().Where(c => !c.Eliminado && c.EstadoPublicacion != null && c.EstadoPublicacion != 0).CountAsync();
            var totalUrls = contentsCount + 1; // + home page

            if (totalUrls <= MaxUrlsPerSitemap)
            {
                var xml = await GenerateSitemapPageXml(0);
                _cache.Set(CacheKeyIndex, xml, TimeSpan.FromMinutes(30));
                return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
            }



            var pages = (int)Math.Ceiling(totalUrls / (double)MaxUrlsPerSitemap);
            var hostBase = GetHostBase();

            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            }))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

                for (int i = 1; i <= pages; i++)
                {
                    xw.WriteStartElement("sitemap");
                    xw.WriteElementString("loc", $"{hostBase}/sitemap-{i}.xml");
                    xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

                var indexXml = sb.ToString();
                _cache.Set(CacheKeyIndex, indexXml, TimeSpan.FromMinutes(30));
                return File(Encoding.UTF8.GetBytes(indexXml), "application/xml; charset=utf-8");
        }

        // GET /sitemap-{page}.xml  (page is 1-based)
        [HttpGet("sitemap-{page:int}.xml")]
        public async Task<IActionResult> Page(int page)
        {
            if (page < 1) return NotFound();

            var cacheKey = CacheKeyPagePrefix + page;
            if (_cache.TryGetValue<string>(cacheKey, out var cached))
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");

            var xml = await GenerateSitemapPageXml(page - 1);
            _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        // Master index moved to dedicated SitemapIndexController to avoid route conflicts

        [HttpPost("admin/sitemap/refresh")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                // Remove index
                _cache.Remove(CacheKeyIndex);

                // Remove a reasonable range of page cache entries (1..100)
                for (int i = 1; i <= 100; i++)
                {
                    _cache.Remove(CacheKeyPagePrefix + i);
                }

                // Optionally compute counts to return to client
                var contentsCount = await _db.Contenidos.AsNoTracking().Where(c => !c.Eliminado).CountAsync();
                var preguntasCount = await _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado).CountAsync();
                var categoriesCount = await _db.ContenidosCategorias.AsNoTracking().Where(c => !c.Borrado && !string.IsNullOrWhiteSpace(c.CategoriaSlug)).CountAsync();

                _logger.LogInformation("Sitemap cache invalidated by {User}", User?.Identity?.Name ?? "unknown");
                return Json(new
                {
                    refreshed = true,
                    counts = new
                    {
                        contents = contentsCount,
                        preguntas = preguntasCount,
                        categorias = categoriesCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating sitemap cache");
                return StatusCode(500, new { refreshed = false, error = ex.Message });
            }
        }

        [NonAction]
        public void InvalidateCache()
        {
            _cache.Remove(CacheKeyIndex);
            for (int i = 1; i <= 100; i++)
            {
                _cache.Remove(CacheKeyPagePrefix + i);
            }
            _logger.LogInformation("Sitemap cache invalidated.");
        }

        // Lightweight endpoint for UI to invalidate contents sitemap cache
        [HttpPost("sitemap/refresh")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult RefreshFromUi()
        {
            try
            {
                _cache.Remove(CacheKeyIndex);
                for (int i = 1; i <= 100; i++) _cache.Remove(CacheKeyPagePrefix + i);
                _logger.LogInformation("Sitemap cache invalidated via UI by {User}", User?.Identity?.Name ?? "unknown");
                return Json(new { refreshed = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating sitemap cache via UI");
                return StatusCode(500, new { refreshed = false, error = ex.Message });
            }
        }

        private async Task<string> GenerateSitemapPageXml(int pageIndex)
        {
            var hostBase = GetHostBase();
            var skip = pageIndex * MaxUrlsPerSitemap;
            var take = MaxUrlsPerSitemap;
            var entries = new List<UrlEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1) Contenidos (solo publicados y no eliminados) — usar slugs y categoria inmediata para URL SEO
            var contents = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado && c.EstadoPublicacion != null && c.EstadoPublicacion != 0)
                .OrderBy(c => c.Id)
                .Skip(skip)
                .Take(take)
                .Select(c => new
                {
                    c.Id,
                    Slug = c.ContenidoTituloSlug,
                    LastMod = (DateTimeOffset?)c.FechaModificado ?? (DateTimeOffset?)c.FechaCreado
                })
                .ToListAsync();

            if (contents.Any())
            {
                var ids = contents.Select(c => c.Id).ToList();
                var cats = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                    .Where(r => ids.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Join(_db.ContenidosCategorias.AsNoTracking(),
                          rel => rel.IdCategoria,
                          cat => cat.Sequence,
                              (rel, cat) => new { rel.IdContenido, cat.CategoriaSlug, cat.CategoriaPadre, rel.EsPrincipal })
                    .ToListAsync();

                var catLookup = cats.GroupBy(x => x.IdContenido).ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(x => x.EsPrincipal == true) // prefer principal relation when present
                        .ThenBy(x => x.CategoriaPadre.HasValue ? 0 : 1) // prefer child categories
                        .FirstOrDefault()?.CategoriaSlug
                );

                foreach (var c in contents)
                {
                    // Skip entries with empty slug to avoid URLs like /c/
                    if (string.IsNullOrWhiteSpace(c.Slug)) continue;

                    string loc;
                    if (catLookup.TryGetValue(c.Id, out var catSlug) && !string.IsNullOrWhiteSpace(catSlug))
                    {
                        var catSeg = Uri.EscapeUriString(catSlug.Trim('/'));
                        loc = $"{hostBase}/{catSeg}/{Uri.EscapeUriString(c.Slug)}";
                    }
                    else
                    {
                        loc = $"{hostBase}/c/{Uri.EscapeUriString(c.Slug)}";
                    }

                    if (seen.Add(loc))
                    {
                        entries.Add(new UrlEntry { Loc = loc, LastMod = c.LastMod, ChangeFreq = "weekly", Priority = "0.8" });
                    }
                }
            }

            // 5) Static pages (only add the home page as first entry)
            if (pageIndex == 0 && entries.Count < MaxUrlsPerSitemap)
            {
                var home = new UrlEntry { Loc = $"{hostBase}/", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "1.0" };
                if (seen.Add(home.Loc)) entries.Insert(0, home);
            }

            return BuildXml(entries);
        }

        private string BuildXml(List<UrlEntry> entries)
        {
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = false
            }))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

                foreach (var e in entries)
                {
                    xw.WriteStartElement("url");
                    xw.WriteElementString("loc", e.Loc);
                    if (e.LastMod.HasValue)
                        xw.WriteElementString("lastmod", e.LastMod.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    if (!string.IsNullOrWhiteSpace(e.ChangeFreq))
                        xw.WriteElementString("changefreq", e.ChangeFreq);
                    if (!string.IsNullOrWhiteSpace(e.Priority))
                        xw.WriteElementString("priority", e.Priority);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var xml = sb.ToString();

            // Ensure XML declaration uses UTF-8 and normalize any accidental non-breaking spaces
            xml = xml.Replace(((char)0x00A0).ToString(), " ");
            if (xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                // replace encoding value if present
                xml = System.Text.RegularExpressions.Regex.Replace(xml, "encoding=\".*?\"", "encoding=\"UTF-8\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            else
            {
                xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xml;
            }

            return xml;
        }

        private string GetHostBase()
        {
            // Respect reverse proxy headers (X-Forwarded-Proto) and prefer https in production
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            var scheme = !string.IsNullOrWhiteSpace(forwardedProto) ? forwardedProto : Request.Scheme;

            // If scheme resolved to http but the request isn't explicitly insecure, prefer https
            if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase))
            {
                scheme = "https";
            }

            var host = Request.Host.HasValue ? Request.Host.Value : "eiibd.com";
            return $"{scheme}://{host}";
        }

        private class UrlEntry
        {
            public string Loc { get; set; }
            public DateTimeOffset? LastMod { get; set; }
            public string ChangeFreq { get; set; }
            public string Priority { get; set; }
        }
    }
}