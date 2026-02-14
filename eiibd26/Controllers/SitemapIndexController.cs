using System;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace eiibd26.Controllers
{
    [Route("")]
    [AllowAnonymous]
    public class SitemapIndexController : Controller
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<SitemapIndexController> _logger;
        private const string CacheKey = "sitemap_index_master_xml";

        public SitemapIndexController(IMemoryCache cache, ILogger<SitemapIndexController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // GET /sitemap-index.xml
        [HttpGet("sitemap-index.xml")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Index()
        {
            if (_cache.TryGetValue<string>(CacheKey, out var cached))
            {
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");
            }

            var hostBase = GetHostBase();

            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8, OmitXmlDeclaration = false }))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("sitemapindex", "http://www.sitemaps.org/schemas/sitemap/0.9");

                // Reference the main sitemaps
                xw.WriteStartElement("sitemap");
                xw.WriteElementString("loc", $"{hostBase}/sitemap.xml");
                xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                xw.WriteEndElement();

                xw.WriteStartElement("sitemap");
                xw.WriteElementString("loc", $"{hostBase}/sitemap-preguntas.xml");
                xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                xw.WriteEndElement();

                xw.WriteStartElement("sitemap");
                xw.WriteElementString("loc", $"{hostBase}/sitemap-usuarios.xml");
                xw.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                xw.WriteEndElement();

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var xml = sb.ToString();

            // Normalize non-breaking spaces
            xml = xml.Replace(((char)0x00A0).ToString(), " ");

            // Remove BOM or any accidental invisible characters at the start
            xml = xml.TrimStart('\uFEFF', '\u0000', '\u0001', '\u0002', ' ', '\r', '\n', '\t');

            // Ensure XML declaration exists and uses UTF-8
            if (xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
            {
                xml = System.Text.RegularExpressions.Regex.Replace(xml, "encoding=\".*?\"", "encoding=\"UTF-8\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Ensure declaration is followed by a single newline
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

            _cache.Set(CacheKey, xml, TimeSpan.FromMinutes(30));
            _logger.LogInformation("Generated sitemap-index.xml");
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        private string GetHostBase()
        {
            // Use the canonical production host for sitemap index so search engines see the correct domain.
            return "https://eiibd.com";
        }
    }
}
