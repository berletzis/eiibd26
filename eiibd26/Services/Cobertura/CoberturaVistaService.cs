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

        // Umbrales de vista (embeddings), provisionales — a calibrar con datos reales:
        //   "Artículos Similares" (subtema fino) = Score ≥ 0.78
        //   "Explora el tema"     (mismo tema)   = 0.55 ≤ Score < 0.78
        private const decimal UmbralSimilares = 0.78m;
        private const decimal UmbralArea = 0.55m;

        // Panel admin "matriz de huecos": motor + umbrales por escala (NO comparables 1:1).
        //   embeddings: cubierto ≥ 0.78 · área/oportunidad 0.55–0.78 · hueco < 0.55
        //   firma:      cubierto ≥ 0.60 · débil 0.50–0.60 · hueco sin match (persistencia ≥ 0.50)
        // Umbrales EDITORIALES (admin: panel de cobertura + vista Oportunidades). Independientes
        // del paciente: arrancan con los mismos valores (0.78/0.55) pero ya NO aliasan
        // UmbralArea/UmbralSimilares, así se pueden tunear sin afectar lo que ve el paciente
        // en "En otros sitios"/Radar/"Similares de EIIBD".
        private const decimal CobEmbCubiertoEditorial = 0.78m; // ≥ esto = cubierto
        private const decimal CobEmbFloorEditorial    = 0.55m; // bajo esto = hueco
        private const decimal CobFirCubierto = 0.60m;           // firma sin cambios
        private const decimal CobFirFloor    = 0.50m;           // = CosenoMin firma (no filtra extra)

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

        public async Task<SimilaresPagina> ObtenerSimilaresAsync(
            int contenidoId, string tab, int offset, int take, CancellationToken ct = default)
        {
            if (take <= 0) take = 5;
            if (offset < 0) offset = 0;
            if (!await EsArticuloAsync(contenidoId, ct))
                return new SimilaresPagina();

            // Dos umbrales sobre el MISMO motor de embeddings:
            //   "similares" (subtema fino) = Score >= 0.78
            //   "area"      (mismo tema)   = 0.55 <= Score < 0.78
            var (minInc, maxExc) = tab switch
            {
                "similares" => (UmbralSimilares, (decimal?)null),        // subtema fino ≥ 0.78
                "area"      => (UmbralArea, UmbralSimilares),            // solo la banda de área
                "externos"  => (UmbralArea, (decimal?)null),            // TODOS los externos ≥ 0.55 (lista única del sidebar)
                _           => (UmbralSimilares, (decimal?)null)
            };

            var q =
                from cs in _db.CoberturaSimilitudesEmbedding.AsNoTracking()
                join sp in _db.ScrapedPagesRef.AsNoTracking() on cs.BId equals sp.ScrapedPageId
                where cs.TipoPar == TipoParSimilitud.PropioExterno && cs.AId == contenidoId
                      && cs.Score >= minInc && (maxExc == null || cs.Score < maxExc)
                orderby cs.Score descending, cs.BId
                select new { sp.ScrapedPageId, sp.Url, sp.SourceSiteId, cs.Score };

            // Pide take+1 para saber si quedan más (barato: una sola query, sin COUNT aparte).
            var rows = await q.Skip(offset).Take(take + 1).ToListAsync(ct);
            var hasMore = rows.Count > take;
            if (hasMore) rows = rows.Take(take).ToList();

            if (rows.Count == 0)
                return new SimilaresPagina { Items = Array.Empty<ExternoSimilarDto>(), HasMore = false, NextOffset = offset };

            var sitios = await NombresSitiosAsync(rows.Select(p => p.SourceSiteId), ct);

            var items = rows.Select(p => new ExternoSimilarDto
            {
                ScrapedPageId = p.ScrapedPageId,
                Url = p.Url,
                SitioNombre = sitios.TryGetValue(p.SourceSiteId, out var n) ? n : Dominio(p.Url),
                Titulo = TituloDesdeUrl(p.Url),
                Score = (double)p.Score
            }).ToList();

            return new SimilaresPagina { Items = items, HasMore = hasMore, NextOffset = offset + items.Count };
        }

        public async Task<IReadOnlyList<SimilarPropioDto>> ObtenerSimilaresPropiosAsync(
            int contenidoId, int take, CancellationToken ct = default)
        {
            if (take <= 0) take = 6;

            // Solo artículos reales (árbol General) en ambos extremos: no recomendar páginas de sistema.
            var articuloIds = (await ArticuloIdsAsync(ct)).ToHashSet();
            if (!articuloIds.Contains(contenidoId)) return Array.Empty<SimilarPropioDto>();

            // Pares propio↔propio que tocan este artículo (pocos: ~decenas). El artículo puede ser A o B.
            var pares = await _db.CoberturaSimilitudesEmbedding.AsNoTracking()
                .Where(cs => cs.TipoPar == TipoParSimilitud.PropioPropio
                             && (cs.AId == contenidoId || cs.BId == contenidoId))
                .Select(cs => new { cs.AId, cs.BId, cs.Score })
                .ToListAsync(ct);

            return pares
                .Select(p => new { OtherId = p.AId == contenidoId ? p.BId : p.AId, Score = (double)p.Score })
                .Where(x => x.OtherId != contenidoId && articuloIds.Contains(x.OtherId))
                .GroupBy(x => x.OtherId).Select(g => g.OrderByDescending(x => x.Score).First())
                .OrderByDescending(x => x.Score)
                .Take(take)
                .Select(x => new SimilarPropioDto { Id = x.OtherId, Score = x.Score })
                .ToList();
        }

        // ---- Vista admin ----

        public async Task<IReadOnlyList<CoberturaTemaDto>> ObtenerCoberturaTemasAsync(string? orden, string? motor = null, CancellationToken ct = default)
        {
            var usarFirma   = string.Equals(motor, "firma", StringComparison.OrdinalIgnoreCase);
            var floor       = usarFirma ? CobFirFloor : CobEmbFloorEditorial;
            var cubiertoMin = usarFirma ? CobFirCubierto : CobEmbCubiertoEditorial;
            var articuloIds = await ArticuloIdsAsync(ct);

            // Pares propio-externo donde el propio es ARTÍCULO (excluye páginas de sistema del lado propio),
            // del motor elegido y sobre el piso de cobertura. Embeddings por defecto; ?motor=firma = fallback en vivo.
            var pares = usarFirma
                ? await _db.CoberturaSimilitudes.AsNoTracking()
                    .Where(cs => cs.TipoPar == TipoParSimilitud.PropioExterno && articuloIds.Contains(cs.AId) && cs.Score >= floor)
                    .Select(cs => new { cs.BId, cs.AId, cs.Score }).ToListAsync(ct)
                : await _db.CoberturaSimilitudesEmbedding.AsNoTracking()
                    .Where(cs => cs.TipoPar == TipoParSimilitud.PropioExterno && articuloIds.Contains(cs.AId) && cs.Score >= floor)
                    .Select(cs => new { cs.BId, cs.AId, cs.Score }).ToListAsync(ct);

            var mejorPorExterno = pares
                .GroupBy(p => p.BId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).First());

            // Universo de temas externos: los indexados por el motor activo (firma o embedding).
            var externos = usarFirma
                ? await _db.ScrapedPagesRef.AsNoTracking().Where(s => s.Firma != null)
                    .Select(s => new { s.ScrapedPageId, s.Url, s.SourceSiteId, s.Language }).ToListAsync(ct)
                : await _db.ScrapedPagesRef.AsNoTracking().Where(s => s.Embedding != null && s.Embedding != "[]")
                    .Select(s => new { s.ScrapedPageId, s.Url, s.SourceSiteId, s.Language }).ToListAsync(ct);

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
                    MejorArticuloTitulo = best != null && titulosArticulo.TryGetValue(best.AId, out var t) ? t : null,
                    Estado = best == null ? "Hueco" : (best.Score >= cubiertoMin ? "Cubierto" : "Débil")
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
