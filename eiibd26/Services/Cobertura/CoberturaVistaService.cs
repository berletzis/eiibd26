using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Cobertura;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Cobertura
{
    /// <inheritdoc/>
    public sealed class CoberturaVistaService : ICoberturaVistaService
    {
        private readonly ApplicationDbContext _db;

        // Umbral de visualización paciente (más alto que el 0.50 del cálculo: solo relaciones fuertes).
        private const double MinScorePaciente = 0.60;
        private const int MaxExternosPaciente = 5;

        // Raíz del árbol de categorías "artículo real".
        private const int CategoriaGeneral = 1;

        // El set de categorías-artículo es configuración estática: cache en memoria con refresco perezoso.
        private static HashSet<int>? _catsArticulo;
        private static DateTime _catsCargadoUtc;
        private static readonly object _catsLock = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        public CoberturaVistaService(ApplicationDbContext db) => _db = db;

        // ---- Filtro de artículo (árbol General) ----

        private async Task<HashSet<int>> CategoriasArticuloAsync(CancellationToken ct)
        {
            lock (_catsLock)
            {
                if (_catsArticulo != null && DateTime.UtcNow - _catsCargadoUtc < CacheTtl)
                    return _catsArticulo;
            }

            var cats = await _db.ContenidosCategorias.AsNoTracking()
                .Where(c => !c.Borrado)
                .Select(c => new { c.Sequence, c.CategoriaPadre })
                .ToListAsync(ct);

            // BFS desde General (Sequence=1) por CategoriaPadre.
            var porPadre = cats.ToLookup(c => c.CategoriaPadre ?? 0);
            var set = new HashSet<int>();
            var cola = new Queue<int>();
            cola.Enqueue(CategoriaGeneral);
            while (cola.Count > 0)
            {
                var s = cola.Dequeue();
                if (!set.Add(s)) continue;
                foreach (var h in porPadre[s]) cola.Enqueue(h.Sequence);
            }

            lock (_catsLock) { _catsArticulo = set; _catsCargadoUtc = DateTime.UtcNow; }
            return set;
        }

        private async Task<List<int>> ArticuloIdsAsync(CancellationToken ct)
        {
            var cats = await CategoriasArticuloAsync(ct);
            return await _db.ContenidosCategoriasRelacion.AsNoTracking()
                .Where(r => !r.Borrado && r.IdCategoria != null && cats.Contains(r.IdCategoria.Value))
                .Select(r => r.IdContenido)
                .Distinct()
                .ToListAsync(ct);
        }

        public async Task<bool> EsArticuloAsync(int contenidoId, CancellationToken ct = default)
        {
            var cats = await CategoriasArticuloAsync(ct);
            return await _db.ContenidosCategoriasRelacion.AsNoTracking()
                .AnyAsync(r => r.IdContenido == contenidoId && !r.Borrado
                            && r.IdCategoria != null && cats.Contains(r.IdCategoria.Value), ct);
        }

        // ---- Vista paciente ----

        public async Task<IReadOnlyList<ExternoSimilarDto>> ObtenerExternosSimilaresAsync(int contenidoId, CancellationToken ct = default)
        {
            if (!await EsArticuloAsync(contenidoId, ct))
                return Array.Empty<ExternoSimilarDto>();

            var min = (decimal)MinScorePaciente;
            var pares = await (
                from cs in _db.CoberturaSimilitudes.AsNoTracking()
                join sp in _db.ScrapedPagesRef.AsNoTracking() on cs.BId equals sp.ScrapedPageId
                where cs.TipoPar == TipoParSimilitud.PropioExterno && cs.AId == contenidoId && cs.Score >= min
                orderby cs.Score descending
                select new { sp.ScrapedPageId, sp.Url, sp.SourceSiteId, cs.Score })
                .Take(MaxExternosPaciente)
                .ToListAsync(ct);

            if (pares.Count == 0) return Array.Empty<ExternoSimilarDto>();

            var sitios = await NombresSitiosAsync(pares.Select(p => p.SourceSiteId), ct);

            return pares.Select(p => new ExternoSimilarDto
            {
                ScrapedPageId = p.ScrapedPageId,
                Url = p.Url,
                SitioNombre = sitios.TryGetValue(p.SourceSiteId, out var n) ? n : Dominio(p.Url),
                Titulo = TituloDesdeUrl(p.Url),
                Score = (double)p.Score
            }).ToList();
        }

        // ---- Vista admin ----

        public async Task<IReadOnlyList<CoberturaTemaDto>> ObtenerCoberturaTemasAsync(string? orden, CancellationToken ct = default)
        {
            var articuloIds = await ArticuloIdsAsync(ct);

            // Pares propio-externo donde el propio es ARTÍCULO (excluye páginas de sistema del lado propio).
            var pares = await _db.CoberturaSimilitudes.AsNoTracking()
                .Where(cs => cs.TipoPar == TipoParSimilitud.PropioExterno && articuloIds.Contains(cs.AId))
                .Select(cs => new { cs.BId, cs.AId, cs.Score })
                .ToListAsync(ct);

            var mejorPorExterno = pares
                .GroupBy(p => p.BId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).First());

            // Todos los externos indexados (con firma).
            var externos = await _db.ScrapedPagesRef.AsNoTracking()
                .Where(s => s.Firma != null)
                .Select(s => new { s.ScrapedPageId, s.Url, s.SourceSiteId, s.Language })
                .ToListAsync(ct);

            var sitios = await NombresSitiosAsync(externos.Select(e => e.SourceSiteId), ct);

            var titulosArticulo = await _db.Contenidos.AsNoTracking()
                .Where(c => articuloIds.Contains(c.Id))
                .Select(c => new { c.Id, c.ContenidoTitulo })
                .ToDictionaryAsync(c => c.Id, c => c.ContenidoTitulo ?? $"Artículo {c.Id}", ct);

            var filas = externos.Select(e =>
            {
                mejorPorExterno.TryGetValue(e.ScrapedPageId, out var best);
                return new CoberturaTemaDto
                {
                    ScrapedPageId = e.ScrapedPageId,
                    Url = e.Url,
                    SitioNombre = sitios.TryGetValue(e.SourceSiteId, out var n) ? n : Dominio(e.Url),
                    Titulo = TituloDesdeUrl(e.Url),
                    Idioma = e.Language ?? "",
                    MejorScore = best == null ? (double?)null : (double)best.Score,
                    MejorArticuloId = best?.AId,
                    MejorArticuloTitulo = best != null && titulosArticulo.TryGetValue(best.AId, out var t) ? t : null
                };
            });

            // Orden: "huecos" primero (sin match, luego score asc) o "cubiertos" primero (score desc). Default cubiertos.
            filas = string.Equals(orden, "huecos", StringComparison.OrdinalIgnoreCase)
                ? filas.OrderBy(f => f.MejorScore.HasValue).ThenBy(f => f.MejorScore ?? 0)
                : filas.OrderByDescending(f => f.MejorScore ?? -1);

            return filas.ToList();
        }

        // ---- Helpers ----

        private async Task<Dictionary<int, string>> NombresSitiosAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var distintos = ids.Distinct().ToList();
            return await _db.SourceSitesRef.AsNoTracking()
                .Where(s => distintos.Contains(s.SourceSiteId))
                .ToDictionaryAsync(s => s.SourceSiteId, s => s.Name, ct);
        }

        /// <summary>Deriva un título legible del último segmento del slug de la URL (no hay TitleRaw).</summary>
        internal static string TituloDesdeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "(sin título)";
            var u = url.Split('?', '#')[0].TrimEnd('/');
            var slug = u.Length == 0 ? url : u[(u.LastIndexOf('/') + 1)..];
            if (string.IsNullOrWhiteSpace(slug)) return Dominio(url);
            slug = Uri.UnescapeDataString(slug).Replace('-', ' ').Replace('_', ' ').Trim();
            if (slug.Length == 0) return Dominio(url);
            return char.ToUpper(slug[0]) + slug[1..];
        }

        private static string Dominio(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return "";
            return Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.Host : url;
        }
    }
}
