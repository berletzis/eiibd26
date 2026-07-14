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
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Services.Platillos;

namespace eiibd26.Controllers
{
    // Sitemap de la seccion Platillos: el listado + una URL por ingrediente que TENGA nota clinica
    // visible (revisada por medico), propia o de su grupo. Catalogo chico → un solo urlset, sin paginar.
    // Regla: una pagina sin contenido revisado no se indexa NI aparece en el sitemap (mismo candado
    // que la vista). Asi el sitemap se auto-puebla cuando un medico aprueba, sin tocar codigo.
    [Route("")]
    [AllowAnonymous]
    public class SitemapPlatillosController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly IPlatNotaClinicaService _notas;
        private const string CacheKey = "sitemap_platillos_xml";

        public SitemapPlatillosController(ApplicationDbContext db, IMemoryCache cache, IPlatNotaClinicaService notas)
        {
            _db = db;
            _cache = cache;
            _notas = notas;
        }

        // GET /sitemap-platillos.xml
        [HttpGet("sitemap-platillos.xml")]
        public async Task<IActionResult> Index()
        {
            if (_cache.TryGetValue<string>(CacheKey, out var cached))
                return File(Encoding.UTF8.GetBytes(cached), "application/xml; charset=utf-8");

            var hostBase = GetHostBase();

            var ingredientes = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new { i.Id, i.Nombre, i.GrupoId })
                .ToListAsync();

            // Mismo candado que la vista: incluir un ingrediente solo si el o su grupo tienen nota visible.
            var ingConNota = await _notas.ObtenerDestinosConNotaVisibleAsync("Ingrediente");
            var grpConNota = await _notas.ObtenerDestinosConNotaVisibleAsync("Grupo");

            var entries = new List<(string Loc, string ChangeFreq, string Priority)>
            {
                ($"{hostBase}/Platillos", "weekly", "0.7")
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ing in ingredientes)
            {
                // Sin nota revisada (propia o de grupo) → fuera del sitemap.
                if (!ingConNota.Contains(ing.Id) && !grpConNota.Contains(ing.GrupoId)) continue;

                var slug = SlugHelper.GenerateSlug(ing.Nombre);
                if (string.IsNullOrWhiteSpace(slug)) continue;
                var loc = $"{hostBase}/Platillos/Ingrediente/{Uri.EscapeDataString(slug)}";
                if (seen.Add(loc))
                    entries.Add((loc, "monthly", "0.6"));
            }

            // Detalle de platillos: activos y con >=1 ingrediente (misma regla de seguridad que el
            // listado y la vista). Contenido real (ingredientes + pasos + fuente) → siempre indexable.
            var platConIng = await _db.PlatPlatilloIngredientes.AsNoTracking()
                .Select(pi => pi.PlatilloId).Distinct().ToListAsync();
            var platillos = await _db.PlatPlatillos.AsNoTracking()
                .Where(p => p.Activo && platConIng.Contains(p.Id))
                .OrderBy(p => p.Codigo)
                .Select(p => p.Nombre)
                .ToListAsync();
            foreach (var nombre in platillos)
            {
                var slug = SlugHelper.GenerateSlug(nombre);
                if (string.IsNullOrWhiteSpace(slug)) continue;
                var loc = $"{hostBase}/Platillos/{Uri.EscapeDataString(slug)}";
                if (seen.Add(loc))
                    entries.Add((loc, "weekly", "0.6"));
            }

            var xml = BuildXml(entries);
            _cache.Set(CacheKey, xml, TimeSpan.FromMinutes(30));
            return File(Encoding.UTF8.GetBytes(xml), "application/xml; charset=utf-8");
        }

        private static string BuildXml(List<(string Loc, string ChangeFreq, string Priority)> entries)
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
                    xw.WriteElementString("changefreq", e.ChangeFreq);
                    xw.WriteElementString("priority", e.Priority);
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }

            var xml = sb.ToString().Replace(((char)0x00A0).ToString(), " ");
            if (xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
                xml = System.Text.RegularExpressions.Regex.Replace(xml, "encoding=\".*?\"", "encoding=\"UTF-8\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            else
                xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + xml;
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
    }
}
