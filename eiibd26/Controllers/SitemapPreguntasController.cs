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

namespace eiibd26.Controllers
{
    // Sitemap específico para la sección "Preguntas"
    // Rutas:
    //  - /sitemap-preguntas.xml        -> índice o único sitemap de preguntas
    //  - /sitemap-preguntas-{n}.xml    -> páginas (1-based)
    [Route("")]
    public class SitemapPreguntasController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapPreguntasController> _logger;

        private const int MaxUrlsPerSitemap = 45000;
        private const string CacheKeyIndex = "sitemap_preguntas_index_xml";
        private const string CacheKeyPagePrefix = "sitemap_preguntas_page_xml_";

        public SitemapPreguntasController(ApplicationDbContext db, IMemoryCache cache, ILogger<SitemapPreguntasController> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        // GET /sitemap-preguntas.xml
        [HttpGet("sitemap-preguntas.xml")]
        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<string>(CacheKeyIndex, out var cachedIndex))
                return Content(cachedIndex, "application/xml", Encoding.UTF8);

            var preguntasCount = await _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado).CountAsync();
            var totalUrls = preguntasCount + 5; // + algunas páginas estáticas relacionadas (opcional)

            if (totalUrls <= MaxUrlsPerSitemap)
            {
                var xml = await GeneratePreguntasSitemapPageXml(0);
                _cache.Set(CacheKeyIndex, xml, TimeSpan.FromMinutes(30));
                return Content(xml, "application/xml", Encoding.UTF8);
            }

            var pages = (int)Math.Ceiling(totalUrls / (double)MaxUrlsPerSitemap);
            var hostBase = GetHostBase();

            using var sw = new StringWriter();
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

                for (int i = 1; i <= pages; i++)
                {
                    xw.WriteStartElement("sitemap");
                    xw.WriteElementString("loc", $"{hostBase}/sitemap-preguntas-{i}.xml");
                    xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var indexXml = sw.ToString();
            _cache.Set(CacheKeyIndex, indexXml, TimeSpan.FromMinutes(30));
            return Content(indexXml, "application/xml", Encoding.UTF8);
        }

        // GET /sitemap-preguntas-{page}.xml  (page is 1-based)
        [HttpGet("sitemap-preguntas-{page}.xml")]
        public async Task<IActionResult> Page(int page)
        {
            if (page < 1) return NotFound();

            var cacheKey = CacheKeyPagePrefix + page;
            if (_cache.TryGetValue<string>(cacheKey, out var cached))
                return Content(cached, "application/xml", Encoding.UTF8);

            var xml = await GeneratePreguntasSitemapPageXml(page - 1);
            _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(30));
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // Genera un urlset para la página indicada (0-based pageIndex)
        private async Task<string> GeneratePreguntasSitemapPageXml(int pageIndex)
        {
            var hostBase = GetHostBase();
            var skip = pageIndex * MaxUrlsPerSitemap;
            var take = MaxUrlsPerSitemap;

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

            var entries = preguntas.Select(q => new UrlEntry
            {
                Loc = $"{hostBase}/Preguntas/{Uri.EscapeDataString(q.Slug)}",
                LastMod = q.LastMod,
                ChangeFreq = "weekly",
                Priority = "0.5"
            }).ToList();

            // Si es la primera página, puedes añadir páginas estáticas relacionadas
            if (pageIndex == 0)
            {
                var staticPages = new[]
                {
                    new UrlEntry { Loc = $"{hostBase}/Preguntas", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "0.7" }
                };
                entries.InsertRange(0, staticPages);
            }

            return BuildXml(entries);
        }

        // Construye XML de urlset
        private string BuildXml(List<UrlEntry> entries)
        {
            using var sw = new StringWriter();
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
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

            return sw.ToString();
        }

        private string GetHostBase()
        {
            var scheme = Request.Scheme;
            var host = Request.Host.HasValue ? Request.Host.Value : "localhost";
            return $"{scheme}://{host}";
        }

        // Modelo interno
        private class UrlEntry
        {
            public string Loc { get; set; }
            public DateTimeOffset? LastMod { get; set; }
            public string ChangeFreq { get; set; }
            public string Priority { get; set; }
        }

        // OPTIONAL: public method to invalidate cache programmatically from elsewhere
        // e.g. call this from your admin code when preguntas change:
        [NonAction]
        public void InvalidateCache()
        {
            _cache.Remove(CacheKeyIndex);
            for (int i = 1; i <= 10; i++)
            {
                _cache.Remove(CacheKeyPagePrefix + i);
            }
            _logger.LogInformation("SitemapPreguntas cache invalidated.");
        }
    }
}