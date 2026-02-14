using System;
using System.Collections.Generic;
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

namespace eiibd26.Controllers
{
    [Route("")]
    [AllowAnonymous]
    public class SitemapUsuariosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapUsuariosController> _logger;

        private const int MaxUrlsPerSitemap = 45000;
        private const string CacheKeyIndex = "sitemap_usuarios_index_xml";
        private const string CacheKeyPagePrefix = "sitemap_usuarios_page_xml_";

        public SitemapUsuariosController(ApplicationDbContext db, IMemoryCache cache, ILogger<SitemapUsuariosController> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        // GET /sitemap-usuarios.xml
        [HttpGet("sitemap-usuarios.xml")]
        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<string>(CacheKeyIndex, out var cachedIndex))
                return File(Encoding.UTF8.GetBytes(cachedIndex), "application/xml; charset=utf-8");

            // count only profiles that should appear in search: those with a slug and a name
            var profilesCount = await _db.Perfil.AsNoTracking().Where(p => !string.IsNullOrWhiteSpace(p.slug) && !string.IsNullOrWhiteSpace(p.Nombre)).CountAsync();
            var totalUrls = profilesCount + 1; // + index page for users

            if (totalUrls <= MaxUrlsPerSitemap)
            {
                var xml = await GenerateUsuariosSitemapPageXml(0);
                _cache.Set(CacheKeyIndex, xml, TimeSpan.FromMinutes(30));
                return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
            }

            var pages = (int)Math.Ceiling(totalUrls / (double)MaxUrlsPerSitemap);
            var hostBase = GetHostBase();

            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = false }))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

                for (int i = 1; i <= pages; i++)
                {
                    xw.WriteStartElement("sitemap");
                    xw.WriteElementString("loc", $"{hostBase}/sitemap-usuarios-{i}.xml");
                    xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var indexXml = sb.ToString();
            indexXml = indexXml.Replace(((char)0x00A0).ToString(), " ");
            if (!indexXml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                indexXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + indexXml;

            _cache.Set(CacheKeyIndex, indexXml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(indexXml), "application/xml; charset=utf-8");
        }

        // GET /sitemap-usuarios-{page}.xml  (page is 1-based)
        [HttpGet("sitemap-usuarios-{page:int}.xml")]
        public async Task<IActionResult> Page(int page)
        {
            if (page < 1) return NotFound();

            var cacheKey = CacheKeyPagePrefix + page;
            if (_cache.TryGetValue<string>(cacheKey, out var cached))
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");

            var xml = await GenerateUsuariosSitemapPageXml(page - 1);
            _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        private async Task<string> GenerateUsuariosSitemapPageXml(int pageIndex)
        {
            var hostBase = GetHostBase();
            var skip = pageIndex * MaxUrlsPerSitemap;
            var take = MaxUrlsPerSitemap;

            // Use the same base selection and scoring as Pages/Mapa/Index so the sitemap contains
            // the same users that appear in the map search.
            var now = DateTime.UtcNow;
            var minValidDate = new DateTime(1900, 1, 1);

            var basePerfil = _db.Perfil.AsNoTracking()
                .Where(p => !string.IsNullOrWhiteSpace(p.Latitud) && !string.IsNullOrWhiteSpace(p.Longitud) && !string.IsNullOrWhiteSpace(p.slug) && !string.IsNullOrWhiteSpace(p.Nombre));

            var projectedQuery = basePerfil.Select(p => new
            {
                idUser = p.idUser,
                name = p.Nombre,
                country = p.NombrePais,
                avatar = p.Avatar,
                slug = p.slug,
                fechaCreacion = (DateTime?)p.FechaCreacion,

                condFechaInicio = (DateTime?)_db.condicionUsuario
                    .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                    .OrderByDescending(cu => cu.fechaInicio)
                    .Select(cu => (DateTime?)cu.fechaInicio)
                    .FirstOrDefault(),

                condId = (int?)_db.condicionUsuario
                    .Where(cu => !cu.Eliminado && cu.idUsuario == p.idUser && cu.fechaInicio != null && cu.fechaInicio > minValidDate && cu.fechaInicio <= now)
                    .OrderByDescending(cu => cu.fechaInicio)
                    .Select(cu => (int?)cu.idCondicion)
                    .FirstOrDefault(),

                lastMoodDate = (DateTime?)_db.EstadoAnimoUsuario
                    .Where(m => !m.Eliminado && m.IdUsuario == p.idUser)
                    .OrderByDescending(m => m.FechaRegistro)
                    .Select(m => (DateTime?)m.FechaRegistro)
                    .FirstOrDefault()
            });

            var scored = projectedQuery.Select(x => new
            {
                x.idUser,
                x.name,
                x.country,
                x.avatar,
                x.slug,
                x.fechaCreacion,
                x.condFechaInicio,
                x.condId,
                x.lastMoodDate,
                hasCond = x.condId.HasValue ? 1 : 0,
                avatarReal = ((x.avatar != null) && !x.avatar.ToLower().Contains("default") && !x.avatar.ToLower().Contains("ui-avatars.com")) ? 1 : 0,
                hasLastMood = x.lastMoodDate.HasValue ? 1 : 0,
                hasAbout = (1), // not used here but keep shape
                hasCity = (x.slug != null ? 1 : 0),
                hasCountry = (x.country != null && x.country != "") ? 1 : 0,
                hasLocation = 1
            })
            .Select(x => new
            {
                x.idUser,
                x.name,
                x.country,
                x.avatar,
                x.slug,
                x.fechaCreacion,
                x.condFechaInicio,
                x.condId,
                x.lastMoodDate,
                score = (x.condFechaInicio.HasValue ? 80 : 0)
                        + x.avatarReal * 40
                        + x.hasLastMood * 30
                        + x.hasCond * 30
                        + x.hasCountry * 20
                        + x.hasLocation * 10
            });

            var ordered = scored
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.condFechaInicio)
                .ThenByDescending(x => x.lastMoodDate)
                .ThenByDescending(x => x.fechaCreacion);

            var page = await ordered.Skip(skip).Take(take).ToListAsync();

            var entries = new List<UrlEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in page)
            {
                if (string.IsNullOrWhiteSpace(p.slug)) continue;
                var loc = $"{hostBase}/u/{Uri.EscapeUriString(p.slug)}";
                if (seen.Add(loc))
                {
                    entries.Add(new UrlEntry { Loc = loc, LastMod = p.fechaCreacion.HasValue ? new DateTimeOffset(p.fechaCreacion.Value) : (DateTimeOffset?)null, ChangeFreq = "daily", Priority = "0.8" });
                }
            }

            // add users index as first entry on first page
            // Only add when there are actual user entries — avoid adding a lone "/u" URL that would 404 or be empty
            if (pageIndex == 0 && entries.Count > 0 && entries.Count < MaxUrlsPerSitemap)
            {
                var home = new UrlEntry { Loc = $"{hostBase}/u", LastMod = DateTimeOffset.UtcNow, ChangeFreq = "daily", Priority = "0.8" };
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
                    xw.WriteElementString("lastmod", e.LastMod.Value.ToUniversalTime().ToString("yyyy-MM-dd"));
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
            xml = xml.Replace(((char)0x00A0).ToString(), " ");

            // Trim BOM/leading invisible characters
            xml = xml.TrimStart('\uFEFF', '\u0000', '\u0001', '\u0002', ' ', '\r', '\n', '\t');

            if (xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                xml = System.Text.RegularExpressions.Regex.Replace(xml, "encoding=\".*?\"", "encoding=\"UTF-8\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var m = System.Text.RegularExpressions.Regex.Match(xml, @"^<\?xml.*?\?>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var rest = xml.Substring(m.Length).TrimStart('\r', '\n');
                    xml = m.Value + "\n" + rest;
                }
            }
            else
            {
                xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xml;
            }

            return xml;
        }

        private string GetHostBase()
        {
            var forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
            var scheme = !string.IsNullOrWhiteSpace(forwardedProto) ? forwardedProto : Request.Scheme;
            if (string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase)) scheme = "https";
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
