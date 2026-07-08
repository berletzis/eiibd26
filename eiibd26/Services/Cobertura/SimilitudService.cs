using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Firma;
using eiibd26.Models.Cobertura;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Cobertura
{
    /// <summary>
    /// Progreso en memoria de la corrida de similitud (proceso único IIS), para el polling del panel.
    /// </summary>
    public static class SimilitudProgreso
    {
        public static volatile bool Corriendo;
        public static long TotalEstimado;
        public static long Procesados;
        public static int Guardados;
        public static DateTime? Inicio;
        public static DateTime? Fin;

        public static void Iniciar(long total)
        {
            Corriendo = true; TotalEstimado = total; Procesados = 0; Guardados = 0;
            Inicio = DateTime.Now; Fin = null;
        }
        public static void Actualizar(long procesados, int guardados) { Procesados = procesados; Guardados = guardados; }
        public static void Terminar() { Corriendo = false; Fin = DateTime.Now; }
    }

    /// <inheritdoc cref="ISimilitudService"/>
    public class SimilitudService : ISimilitudService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SimilitudService> _logger;

        private static readonly int[] EstadosVisibles = { 1, 2, 3 };

        // Compuertas de filtrado (Fase 3, afinadas con datos reales — evitan falsos 1.0
        // de firmas pobres). El coseno en sí no se toca.
        private const int RiquezaMin = 4;             // términos distintos mínimos por firma
        private const int TerminosCompartidosMin = 3; // términos compartidos mínimos por par (pre-filtro)
        private const double CosenoMin = 0.50;        // umbral de guardado (fijado con la distribución limpia)

        private const int FlushSize = 1000;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public SimilitudService(ApplicationDbContext db, ILogger<SimilitudService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Firma cargada en memoria (vector disperso + set de términos + magnitud precalculada).
        private sealed class Firmado
        {
            public int Id;
            public DateTime? FirmaEn;
            public string? TituloNorm;   // solo propios (dedup por título); null en externos
            public Dictionary<string, int> Counts = null!;
            public HashSet<string> Terms = null!;
            public double Mag;
        }

        public async Task<(long totalEstimado, int paresGuardados, DateTime? ultimo)> ObtenerProgresoAsync(CancellationToken ct = default)
        {
            var propios = await _db.Contenidos.CountAsync(
                c => !c.Eliminado && c.EstadoPublicacion.HasValue
                     && EstadosVisibles.Contains(c.EstadoPublicacion.Value) && c.Firma != null, ct);
            var externos = await _db.ScrapedPagesRef.CountAsync(s => s.Firma != null, ct);

            long total = (long)propios * externos + (long)propios * (propios - 1) / 2;
            var guardados = await _db.CoberturaSimilitudes.CountAsync(ct);
            var ultimo = await _db.CoberturaSimilitudes.MaxAsync(x => (DateTime?)x.CalculatedAt, ct);
            return (total, guardados, ultimo);
        }

        public async Task<int> CalcularAsync(CancellationToken ct = default)
        {
            var propios = await CargarPropiosAsync(ct);
            var externos = await CargarExternosAsync(ct);

            _logger.LogInformation("[Similitud] Firmados: {P} propios, {E} externos.", propios.Count, externos.Count);

            // Pares existentes (incremental + upsert/borrado). Solo hay filas >umbral, así que es acotado.
            var existentes = await _db.CoberturaSimilitudes.AsNoTracking()
                .Select(r => new { r.Id, r.AId, r.BId, r.TipoPar, r.AFirmaEn, r.BFirmaEn })
                .ToListAsync(ct);
            var mapa = existentes.ToDictionary(r => (r.AId, r.BId, r.TipoPar),
                                               r => (r.Id, r.AFirmaEn, r.BFirmaEn));

            long totalEst = (long)propios.Count * externos.Count + (long)propios.Count * (propios.Count - 1) / 2;
            SimilitudProgreso.Iniciar(totalEst);

            var addBuffer = new List<CoberturaSimilitud>();
            var deleteBuffer = new List<int>();
            long procesados = 0;
            int guardadosNuevos = 0;

            async Task FlushAsync()
            {
                if (deleteBuffer.Count > 0)
                {
                    foreach (var chunk in Particionar(deleteBuffer, 500))
                        await _db.CoberturaSimilitudes.Where(x => chunk.Contains(x.Id)).ExecuteDeleteAsync(ct);
                    deleteBuffer.Clear();
                }
                if (addBuffer.Count > 0)
                {
                    _db.CoberturaSimilitudes.AddRange(addBuffer);
                    await _db.SaveChangesAsync(ct);
                    _db.ChangeTracker.Clear();
                    addBuffer.Clear();
                }
            }

            void Procesar(Firmado a, Firmado b, byte tipo)
            {
                procesados++;

                // Dedup: en propio-propio, saltar duplicados por título (mismo título normalizado).
                if (tipo == TipoParSimilitud.PropioPropio
                    && !string.IsNullOrEmpty(a.TituloNorm) && a.TituloNorm == b.TituloNorm)
                    return;

                var key = (a.Id, b.Id, tipo);
                if (mapa.TryGetValue(key, out var ex))
                {
                    // Incremental: si ambas firmas no cambiaron, no re-calcular.
                    if (ex.AFirmaEn == a.FirmaEn && ex.BFirmaEn == b.FirmaEn) return;
                    // Cambió: borrar la fila vieja (se re-agrega si sigue por encima del umbral).
                    deleteBuffer.Add(ex.Id);
                    mapa.Remove(key);
                }

                // Pre-filtro por solapamiento: exige >= S términos compartidos (evita los
                // falsos 1.0 de firmas pobres que comparten 1 término).
                if (Compartidos(a.Terms, b.Terms) < TerminosCompartidosMin) return;

                var cos = Coseno(a, b);
                if (cos <= CosenoMin) return;            // debajo del umbral de guardado

                addBuffer.Add(new CoberturaSimilitud
                {
                    AId = a.Id,
                    BId = b.Id,
                    TipoPar = tipo,
                    Score = (decimal)Math.Round(cos, 4),
                    AFirmaEn = a.FirmaEn,
                    BFirmaEn = b.FirmaEn,
                    CalculatedAt = DateTime.Now
                });
                guardadosNuevos++;
            }

            try
            {
                for (int i = 0; i < propios.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var a = propios[i];

                    // propio → externos (TipoPar = 2)
                    foreach (var e in externos)
                        Procesar(a, e, TipoParSimilitud.PropioExterno);

                    // propio → propios (j>i, sin duplicar A-B/B-A ni A-A; A queda con el Id menor)
                    for (int j = i + 1; j < propios.Count; j++)
                        Procesar(a, propios[j], TipoParSimilitud.PropioPropio);

                    SimilitudProgreso.Actualizar(procesados, mapa.Count + guardadosNuevos);
                    if (addBuffer.Count >= FlushSize || deleteBuffer.Count >= FlushSize)
                        await FlushAsync();
                }

                await FlushAsync();
            }
            finally
            {
                SimilitudProgreso.Terminar();
            }

            _logger.LogInformation(
                "[Similitud] Corrida completa: {Proc} pares evaluados, {Guard} guardados/refrescados (>{Umbral}).",
                procesados, guardadosNuevos, CosenoMin);
            return guardadosNuevos;
        }

        public async Task<int> ResetearAsync(CancellationToken ct = default)
        {
            var n = await _db.CoberturaSimilitudes.ExecuteDeleteAsync(ct);
            _logger.LogInformation("[Similitud] Reset: {Count} pares borrados.", n);
            return n;
        }

        public async Task<List<TopParDto>> ObtenerTopParesAsync(int limit = 50, CancellationToken ct = default)
        {
            var top = await _db.CoberturaSimilitudes.AsNoTracking()
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new { x.AId, x.BId, x.TipoPar, x.Score, x.CalculatedAt })
                .ToListAsync(ct);

            if (top.Count == 0) return new List<TopParDto>();

            // Etiquetas: propios por título; externos (TipoPar=2) por URL.
            var propioIds = top.Select(t => t.AId)
                .Concat(top.Where(t => t.TipoPar == TipoParSimilitud.PropioPropio).Select(t => t.BId))
                .Distinct().ToList();
            var externoIds = top.Where(t => t.TipoPar == TipoParSimilitud.PropioExterno)
                .Select(t => t.BId).Distinct().ToList();

            var titulos = await _db.Contenidos.AsNoTracking()
                .Where(c => propioIds.Contains(c.Id))
                .Select(c => new { c.Id, c.ContenidoTitulo })
                .ToDictionaryAsync(c => c.Id, c => c.ContenidoTitulo ?? "(sin título)", ct);

            var urls = externoIds.Count == 0
                ? new Dictionary<int, string>()
                : await _db.ScrapedPagesRef.AsNoTracking()
                    .Where(s => externoIds.Contains(s.ScrapedPageId))
                    .Select(s => new { s.ScrapedPageId, s.Url })
                    .ToDictionaryAsync(s => s.ScrapedPageId, s => s.Url, ct);

            return top.Select(t => new TopParDto
            {
                TipoPar = t.TipoPar,
                AId = t.AId,
                ATitulo = titulos.TryGetValue(t.AId, out var at) ? at : $"#{t.AId}",
                BId = t.BId,
                BEtiqueta = t.TipoPar == TipoParSimilitud.PropioPropio
                    ? (titulos.TryGetValue(t.BId, out var bt) ? bt : $"#{t.BId}")
                    : (urls.TryGetValue(t.BId, out var bu) ? bu : $"ScrapedPage #{t.BId}"),
                Score = t.Score,
                CalculatedAt = t.CalculatedAt
            }).ToList();
        }

        // ─────────────────────────── carga / matemática ───────────────────────────

        private async Task<List<Firmado>> CargarPropiosAsync(CancellationToken ct)
        {
            var rows = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value) && c.Firma != null)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Firma, c.FirmaCalculadaEn, c.ContenidoTitulo })
                .ToListAsync(ct);

            return Materializar(rows.Select(r => (r.Id, r.Firma, r.FirmaCalculadaEn, (string?)r.ContenidoTitulo)));
        }

        private async Task<List<Firmado>> CargarExternosAsync(CancellationToken ct)
        {
            var rows = await _db.ScrapedPagesRef.AsNoTracking()
                .Where(s => s.Firma != null)
                .OrderBy(s => s.ScrapedPageId)
                .Select(s => new { s.ScrapedPageId, s.Firma, s.FirmaCalculadaEn })
                .ToListAsync(ct);

            return Materializar(rows.Select(r => (r.ScrapedPageId, r.Firma, r.FirmaCalculadaEn, (string?)null)));
        }

        private static List<Firmado> Materializar(IEnumerable<(int Id, string? Firma, DateTime? FirmaEn, string? Titulo)> rows)
        {
            var list = new List<Firmado>();
            foreach (var r in rows)
            {
                var parsed = Parse(r.Firma);
                if (parsed == null) continue;                        // firma vacía → no comparable
                if (parsed.Value.terms.Count < RiquezaMin) continue; // compuerta de riqueza (R)
                list.Add(new Firmado
                {
                    Id = r.Id,
                    FirmaEn = r.FirmaEn,
                    TituloNorm = r.Titulo == null ? null : FirmaCalculator.Normalizar(r.Titulo),
                    Counts = parsed.Value.counts,
                    Terms = parsed.Value.terms,
                    Mag = parsed.Value.mag
                });
            }
            return list;
        }

        private static (Dictionary<string, int> counts, HashSet<string> terms, double mag)? Parse(string? firmaJson)
        {
            if (string.IsNullOrWhiteSpace(firmaJson)) return null;
            FirmaDto? dto;
            try { dto = JsonSerializer.Deserialize<FirmaDto>(firmaJson, JsonOpts); }
            catch { return null; }
            if (dto?.Counts == null || dto.Counts.Count == 0) return null;

            double sumsq = 0;
            foreach (var v in dto.Counts.Values) sumsq += (double)v * v;
            if (sumsq <= 0) return null;

            return (dto.Counts, new HashSet<string>(dto.Counts.Keys), Math.Sqrt(sumsq));
        }

        private static int Compartidos(HashSet<string> a, HashSet<string> b)
        {
            var (small, large) = a.Count <= b.Count ? (a, b) : (b, a);
            int inter = 0;
            foreach (var t in small) if (large.Contains(t)) inter++;
            return inter;
        }

        private static double Coseno(Firmado a, Firmado b)
        {
            if (a.Mag == 0 || b.Mag == 0) return 0;
            var (small, large) = a.Counts.Count <= b.Counts.Count ? (a.Counts, b.Counts) : (b.Counts, a.Counts);
            double dot = 0;
            foreach (var kv in small)
                if (large.TryGetValue(kv.Key, out var v2)) dot += (double)kv.Value * v2;
            return dot / (a.Mag * b.Mag);
        }

        private static IEnumerable<List<int>> Particionar(List<int> src, int size)
        {
            for (int i = 0; i < src.Count; i += size)
                yield return src.GetRange(i, Math.Min(size, src.Count - i));
        }
    }
}
