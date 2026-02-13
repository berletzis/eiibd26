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

            // contar urls estimadas
            var contentsCount = await _db.Contenidos.AsNoTracking().Where(c => !c.Eliminado).CountAsync();
            var preguntasCount = await _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado).CountAsync();
            var profilesCount = await _db.Perfil.AsNoTracking().Where(p => !string.IsNullOrEmpty(p.slug)).CountAsync();
            var categoriesCount = await _db.ContenidosCategorias.AsNoTracking().Where(c => !c.Borrado && !string.IsNullOrWhiteSpace(c.CategoriaSlug)).CountAsync();

            var totalUrls = contentsCount + preguntasCount + profilesCount + categoriesCount + 10; // + static pages

            if (totalUrls <= MaxUrlsPerSitemap)
            {
                var xml = await GenerateSitemapPageXml(0);
                _cache.Set(CacheKeyIndex, xml, TimeSpan.FromMinutes(30));
                return Content(xml, "application/xml; charset=utf-8");
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
        [HttpGet("sitemap-{page}.xml")]
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

        [HttpPost("admin/sitemap/refresh")]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
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

        private async Task<string> GenerateSitemapPageXml(int pageIndex)
        {
            var hostBase = GetHostBase();
            var skip = pageIndex * MaxUrlsPerSitemap;
            var take = MaxUrlsPerSitemap;
            var entries = new List<UrlEntry>();

            // 1) Contenidos (orden por Id)
            var contents = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado)
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
                          (rel, cat) => new { rel.IdContenido, cat.CategoriaSlug, cat.CategoriaPadre })
                    .ToListAsync();

                var catLookup = cats.GroupBy(x => x.IdContenido).ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1).FirstOrDefault()?.CategoriaSlug
                );

                foreach (var c in contents)
                {
                    string loc;
                    if (catLookup.TryGetValue(c.Id, out var catSlug) && !string.IsNullOrWhiteSpace(catSlug))
                        loc = $"{hostBase}/{catSlug.Trim('/')}/{Uri.EscapeDataString(c.Slug ?? "")}";
                    else
                        loc = $"{hostBase}/c/{Uri.EscapeDataString(c.Slug ?? "")}";

                    entries.Add(new UrlEntry { Loc = loc, LastMod = c.LastMod, ChangeFreq = "weekly", Priority = "0.6" });
                }

                if (entries.Count >= take) return BuildXml(entries);
                take -= entries.Count;
                skip = Math.Max(0, skip - contents.Count);
            }
            else
            {
                var totalContents = await _db.Contenidos.AsNoTracking().Where(c => !c.Eliminado).CountAsync();
                skip = Math.Max(0, skip - totalContents);
            }

            // 2) Preguntas
            if (take > 0)
            {
                var preguntas = await _db.Preguntas.AsNoTracking()
                    .Where(p => !p.Eliminado)
                    .OrderBy(p => p.Id)
                    .Skip(skip)
                    .Take(take)
                    .Select(p => new
                    {
                        Slug = p.Slug ?? p.Id.ToString(),
                        LastMod = (DateTimeOffset?)p.FechaModificacion ?? (DateTimeOffset?)p.FechaCreacion
                    })
                    .ToListAsync();

                foreach (var q in preguntas)
                {
                    entries.Add(new UrlEntry { Loc = $"{hostBase}/Preguntas/{Uri.EscapeDataString(q.Slug)}", LastMod = q.LastMod, ChangeFreq = "weekly", Priority = "0.5" });
                }

                if (entries.Count >= MaxUrlsPerSitemap) return BuildXml(entries);
                take = Math.Max(0, take - preguntas.Count);
                skip = Math.Max(0, skip - preguntas.Count);
            }

            // 3) Perfiles públicos (/u/{slug})
            if (take > 0)
            {
                var profiles = await _db.Perfil.AsNoTracking()
                    .Where(p => !string.IsNullOrWhiteSpace(p.slug))
                    .OrderBy(p => p.idUser)
                    .Skip(skip)
                    .Take(take)
                    .Select(p => new { p.slug, LastMod = (DateTimeOffset?)p.FechaCreacion ?? DateTimeOffset.UtcNow })
                    .ToListAsync();

                foreach (var pr in profiles)
                {
                    entries.Add(new UrlEntry { Loc = $"{hostBase}/u/{Uri.EscapeDataString(pr.slug)}", LastMod = pr.LastMod, ChangeFreq = "monthly", Priority = "0.3" });
                }

                if (entries.Count >= MaxUrlsPerSitemap) return BuildXml(entries);
                take = Math.Max(0, take - profiles.Count);
                skip = Math.Max(0, skip - profiles.Count);
            }

            // 4) Categorías (/{categorySlug})
            if (take > 0)
            {
                var categories = await _db.ContenidosCategorias.AsNoTracking()
                    .Where(c => !c.Borrado && !string.IsNullOrWhiteSpace(c.CategoriaSlug))
                    .OrderBy(c => c.Sequence)
                    .Skip(skip)
                    .Take(take)
                    .Select(c => new { c.CategoriaSlug, LastMod = (DateTimeOffset?)c.FechaModificacion ?? (DateTimeOffset?)c.FechaCreacion })
                    .ToListAsync();

                foreach (var cat in categories)
                {
                    entries.Add(new UrlEntry { Loc = $"{hostBase}/{Uri.EscapeDataString(cat.CategoriaSlug)}", LastMod = cat.LastMod, ChangeFreq = "weekly", Priority = "0.4" });
                }

                if (entries.Count >= MaxUrlsPerSitemap) return BuildXml(entries);
                take = Math.Max(0, take - categories.Count);
            }

            // 5) Static pages (only on first page)
            if (pageIndex == 0 && entries.Count < MaxUrlsPerSitemap)
            {
                var staticPages = new[]
                {
                    new UrlEntry { Loc = $"{hostBase}/", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "1.0" },
                    new UrlEntry { Loc = $"{hostBase}/Contenidos", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "0.8" },
                    new UrlEntry { Loc = $"{hostBase}/Preguntas", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "0.7" }
                };

                foreach (var s in staticPages)
                {
                    if (entries.Count >= MaxUrlsPerSitemap) break;
                    entries.Add(s);
                }
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

            return sb.ToString();
        }

        private string GetHostBase()
        {
            var scheme = Request.Scheme;
            var host = Request.Host.HasValue ? Request.Host.Value : "localhost";
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