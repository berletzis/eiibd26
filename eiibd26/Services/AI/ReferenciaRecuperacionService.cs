using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Configuration;
using eiibd26.Data;
using eiibd26.Models.Cobertura;
using eiibd26.Voyage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eiibd26.Services.AI
{
    /// <inheritdoc cref="IReferenciaRecuperacionService"/>
    public class ReferenciaRecuperacionService : IReferenciaRecuperacionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IVoyageEmbeddingClient _voyage;
        private readonly ReferenciasRecuperacionOptions _opts;
        private readonly ILogger<ReferenciaRecuperacionService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new() { AllowTrailingCommas = true };

        public ReferenciaRecuperacionService(
            ApplicationDbContext db,
            IVoyageEmbeddingClient voyage,
            IOptions<ReferenciasRecuperacionOptions> opts,
            ILogger<ReferenciaRecuperacionService> logger)
        {
            _db = db;
            _voyage = voyage;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<List<ReferenciaCandidataDto>> RecuperarAsync(string consulta, CancellationToken cancellationToken = default)
        {
            var vacio = new List<ReferenciaCandidataDto>();

            // Gates de degradación limpia: sin nada de esto, NO se inventa — se devuelve vacío y la
            // nota queda con su leyenda honesta de siempre.
            if (!_opts.Habilitado || string.IsNullOrWhiteSpace(consulta)) return vacio;
            var dominios = (_opts.DominiosConfiables ?? new List<string>())
                .Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim().ToLowerInvariant()).ToList();
            if (dominios.Count == 0) return vacio;
            if (!_voyage.Habilitado)
            {
                _logger.LogInformation("Recuperación de referencias omitida: Voyage sin API key.");
                return vacio;
            }

            try
            {
                // 1) Embeber la consulta (mismo cliente/modelo con el que el crawler embebió las páginas).
                var qvec = await _voyage.EmbedUnoAsync(consulta, cancellationToken);
                if (qvec == null || qvec.Length == 0) return vacio;
                var qmag = Magnitud(qvec);
                if (qmag <= 0) return vacio;

                // 2) Resolver los sitios de confianza (SourceSitesRef es diminuta → match en memoria).
                var sitios = await _db.SourceSitesRef.AsNoTracking().ToListAsync(cancellationToken);
                var confiables = sitios.Where(s => EsConfiable(s, dominios)).ToList();
                if (confiables.Count == 0)
                {
                    _logger.LogInformation("Ningún SourceSite matchea los dominios de confianza configurados.");
                    return vacio;
                }
                var idsConfiables = confiables.Select(s => s.SourceSiteId).ToHashSet();
                var nombreSitio = confiables
                    .GroupBy(s => s.SourceSiteId)
                    .ToDictionary(g => g.Key, g => g.First().Name);

                // 3) Cargar páginas embebidas SOLO de esos sitios (filtro en SQL por SourceSiteId).
                var paginas = await _db.ScrapedPagesRef.AsNoTracking()
                    .Where(p => p.Embedding != null && idsConfiables.Contains(p.SourceSiteId))
                    .Select(p => new { p.ScrapedPageId, p.SourceSiteId, p.Url, p.Embedding })
                    .ToListAsync(cancellationToken);

                // 4) Coseno en memoria; quedarse con lo que pase el umbral.
                var rank = new List<(int Id, int SiteId, string Url, double Score)>();
                foreach (var p in paginas)
                {
                    var vec = Parse(p.Embedding);
                    if (vec == null || vec.Length != qvec.Length) continue;   // dims distintas → no comparable
                    var mag = Magnitud(vec);
                    if (mag <= 0) continue;
                    var cos = Dot(qvec, vec) / (qmag * mag);
                    if (cos >= _opts.UmbralCoseno) rank.Add((p.ScrapedPageId, p.SourceSiteId, p.Url, cos));
                }
                if (rank.Count == 0) return vacio;

                var top = rank.OrderByDescending(r => r.Score).Take(Math.Max(1, _opts.TopK)).ToList();

                // 5) Título REAL del externo (Article.NormalizedTitle) por join; fallback al slug de la URL.
                var topIds = top.Select(t => t.Id).ToList();
                var titulos = await _db.ArticlesRef.AsNoTracking()
                    .Where(a => a.ScrapedPageId != null && topIds.Contains(a.ScrapedPageId.Value))
                    .GroupBy(a => a.ScrapedPageId!.Value)
                    .Select(g => new { Id = g.Key, Titulo = g.Select(x => x.NormalizedTitle).FirstOrDefault() })
                    .ToDictionaryAsync(x => x.Id, x => x.Titulo, cancellationToken);

                return top.Select(t =>
                {
                    var titulo = titulos.TryGetValue(t.Id, out var tt) && !string.IsNullOrWhiteSpace(tt)
                        ? tt!.Trim()
                        : TituloDesdeUrl(t.Url);
                    var sitio = nombreSitio.TryGetValue(t.SiteId, out var sn) ? sn : "";
                    return new ReferenciaCandidataDto(titulo, t.Url, sitio, t.Score, (int)Math.Round(t.Score * 100));
                }).ToList();
            }
            catch (Exception ex)
            {
                // La recuperación es un extra: nunca debe tumbar la generación de la nota.
                _logger.LogWarning(ex, "Fallo al recuperar referencias candidatas para la consulta.");
                return vacio;
            }
        }

        private static bool EsConfiable(SourceSiteRef s, List<string> dominios)
        {
            var heno = ((s.Name ?? "") + " " + (s.UrlPublica ?? "")).ToLowerInvariant();
            foreach (var dom in dominios)
            {
                if (heno.Contains(dom)) return true;
                var label = dom.Split('.')[0];               // "funeiico.com" → "funeiico"
                if (label.Length > 2 && heno.Contains(label)) return true;
            }
            return false;
        }

        private static string TituloDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var seg = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
                if (string.IsNullOrWhiteSpace(seg)) return uri.Host;
                seg = System.Net.WebUtility.UrlDecode(seg).Replace('-', ' ').Replace('_', ' ').Trim();
                if (seg.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) seg = seg[..^5];
                return string.IsNullOrWhiteSpace(seg) ? uri.Host : seg;
            }
            catch { return url; }
        }

        private static float[]? Parse(string? embJson)
        {
            if (string.IsNullOrWhiteSpace(embJson)) return null;
            try
            {
                var vec = JsonSerializer.Deserialize<float[]>(embJson, JsonOpts);
                return (vec == null || vec.Length == 0) ? null : vec;
            }
            catch { return null; }
        }

        private static double Magnitud(float[] v)
        {
            double s = 0;
            foreach (var x in v) s += (double)x * x;
            return Math.Sqrt(s);
        }

        private static double Dot(float[] a, float[] b)
        {
            double d = 0;
            for (int k = 0; k < a.Length; k++) d += (double)a[k] * b[k];
            return d;
        }
    }
}
