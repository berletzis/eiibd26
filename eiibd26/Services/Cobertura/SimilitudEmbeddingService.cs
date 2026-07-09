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
    /// Progreso en memoria de la corrida de similitud por embeddings (proceso único IIS),
    /// para el polling del panel. Separado del de firma para no mezclar corridas.
    /// </summary>
    public static class SimilitudEmbeddingProgreso
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

    /// <inheritdoc cref="ISimilitudEmbeddingService"/>
    public class SimilitudEmbeddingService : ISimilitudEmbeddingService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SimilitudEmbeddingService> _logger;

        private static readonly int[] EstadosVisibles = { 1, 2, 3 };

        // Umbral de GUARDADO: piso bajo y provisional, para conservar datos de calibración por
        // debajo de los umbrales de vista (0.55 "Explora el tema" / 0.78 "Artículos Similares").
        // Bajarlo requiere "Recalcular total" (el incremental no re-crea pares por debajo del piso).
        private const double CosenoMin = 0.40;

        private const int FlushSize = 1000;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public SimilitudEmbeddingService(ApplicationDbContext db, ILogger<SimilitudEmbeddingService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Embedding cargado en memoria: vector denso + magnitud precalculada.
        private sealed class Embebido
        {
            public int Id;
            public DateTime? EmbEn;
            public string? TituloNorm;   // solo propios (dedup por título); null en externos
            public float[] Vec = null!;
            public double Mag;
        }

        public async Task<(long totalEstimado, int paresGuardados, DateTime? ultimo)> ObtenerProgresoAsync(CancellationToken ct = default)
        {
            var propios = await _db.Contenidos.CountAsync(
                c => !c.Eliminado && c.EstadoPublicacion.HasValue
                     && EstadosVisibles.Contains(c.EstadoPublicacion.Value) && c.Embedding != null, ct);
            var externos = await _db.ScrapedPagesRef.CountAsync(s => s.Embedding != null, ct);

            long total = (long)propios * externos + (long)propios * (propios - 1) / 2;
            var guardados = await _db.CoberturaSimilitudesEmbedding.CountAsync(ct);
            var ultimo = await _db.CoberturaSimilitudesEmbedding.MaxAsync(x => (DateTime?)x.CalculatedAt, ct);
            return (total, guardados, ultimo);
        }

        public async Task<int> CalcularAsync(CancellationToken ct = default)
        {
            var propios = await CargarPropiosAsync(ct);
            var externos = await CargarExternosAsync(ct);

            _logger.LogInformation("[SimEmbedding] Embebidos: {P} propios, {E} externos.", propios.Count, externos.Count);

            var existentes = await _db.CoberturaSimilitudesEmbedding.AsNoTracking()
                .Select(r => new { r.Id, r.AId, r.BId, r.TipoPar, r.AEmbEn, r.BEmbEn })
                .ToListAsync(ct);
            var mapa = existentes.ToDictionary(r => (r.AId, r.BId, r.TipoPar),
                                               r => (r.Id, r.AEmbEn, r.BEmbEn));

            long totalEst = (long)propios.Count * externos.Count + (long)propios.Count * (propios.Count - 1) / 2;
            SimilitudEmbeddingProgreso.Iniciar(totalEst);

            var addBuffer = new List<CoberturaSimilitudEmbedding>();
            var deleteBuffer = new List<int>();
            long procesados = 0;
            int guardadosNuevos = 0;

            async Task FlushAsync()
            {
                if (deleteBuffer.Count > 0)
                {
                    foreach (var chunk in Particionar(deleteBuffer, 500))
                        await _db.CoberturaSimilitudesEmbedding.Where(x => chunk.Contains(x.Id)).ExecuteDeleteAsync(ct);
                    deleteBuffer.Clear();
                }
                if (addBuffer.Count > 0)
                {
                    _db.CoberturaSimilitudesEmbedding.AddRange(addBuffer);
                    await _db.SaveChangesAsync(ct);
                    _db.ChangeTracker.Clear();
                    addBuffer.Clear();
                }
            }

            void Procesar(Embebido a, Embebido b, byte tipo)
            {
                procesados++;

                // Dedup: en propio-propio, saltar duplicados por título (mismo título normalizado).
                if (tipo == TipoParSimilitud.PropioPropio
                    && !string.IsNullOrEmpty(a.TituloNorm) && a.TituloNorm == b.TituloNorm)
                    return;

                var key = (a.Id, b.Id, tipo);
                if (mapa.TryGetValue(key, out var ex))
                {
                    // Incremental: si ambos embeddings no cambiaron, no re-calcular.
                    if (ex.AEmbEn == a.EmbEn && ex.BEmbEn == b.EmbEn) return;
                    // Cambió: borrar la fila vieja (se re-agrega si sigue por encima del umbral).
                    deleteBuffer.Add(ex.Id);
                    mapa.Remove(key);
                }

                // Coseno denso directo — SIN pre-filtros (los de firma no aplican a vectores densos).
                var cos = Coseno(a, b);
                if (cos <= CosenoMin) return;

                addBuffer.Add(new CoberturaSimilitudEmbedding
                {
                    AId = a.Id,
                    BId = b.Id,
                    TipoPar = tipo,
                    Score = (decimal)Math.Round(cos, 5),
                    AEmbEn = a.EmbEn,
                    BEmbEn = b.EmbEn,
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

                    // propio → propios (j>i; A queda con el Id menor)
                    for (int j = i + 1; j < propios.Count; j++)
                        Procesar(a, propios[j], TipoParSimilitud.PropioPropio);

                    SimilitudEmbeddingProgreso.Actualizar(procesados, mapa.Count + guardadosNuevos);
                    if (addBuffer.Count >= FlushSize || deleteBuffer.Count >= FlushSize)
                        await FlushAsync();
                }

                await FlushAsync();
            }
            finally
            {
                SimilitudEmbeddingProgreso.Terminar();
            }

            _logger.LogInformation(
                "[SimEmbedding] Corrida completa: {Proc} pares evaluados, {Guard} guardados/refrescados (>{Umbral}).",
                procesados, guardadosNuevos, CosenoMin);
            return guardadosNuevos;
        }

        public async Task<int> ResetearAsync(CancellationToken ct = default)
        {
            var n = await _db.CoberturaSimilitudesEmbedding.ExecuteDeleteAsync(ct);
            _logger.LogInformation("[SimEmbedding] Reset: {Count} pares borrados.", n);
            return n;
        }

        public async Task<List<TopParDto>> ObtenerTopParesAsync(int limit = 50, CancellationToken ct = default)
        {
            var top = await _db.CoberturaSimilitudesEmbedding.AsNoTracking()
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new { x.AId, x.BId, x.TipoPar, x.Score, x.CalculatedAt })
                .ToListAsync(ct);

            if (top.Count == 0) return new List<TopParDto>();

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

        private async Task<List<Embebido>> CargarPropiosAsync(CancellationToken ct)
        {
            var rows = await _db.Contenidos.AsNoTracking()
                .Where(c => !c.Eliminado && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value) && c.Embedding != null)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Embedding, c.EmbeddingCalculadoEn, c.ContenidoTitulo })
                .ToListAsync(ct);

            return Materializar(rows.Select(r => (r.Id, r.Embedding, r.EmbeddingCalculadoEn, (string?)r.ContenidoTitulo)));
        }

        private async Task<List<Embebido>> CargarExternosAsync(CancellationToken ct)
        {
            var rows = await _db.ScrapedPagesRef.AsNoTracking()
                .Where(s => s.Embedding != null)
                .OrderBy(s => s.ScrapedPageId)
                .Select(s => new { s.ScrapedPageId, s.Embedding, s.EmbeddingCalculadoEn })
                .ToListAsync(ct);

            return Materializar(rows.Select(r => (r.ScrapedPageId, r.Embedding, r.EmbeddingCalculadoEn, (string?)null)));
        }

        private static List<Embebido> Materializar(IEnumerable<(int Id, string? Emb, DateTime? EmbEn, string? Titulo)> rows)
        {
            var list = new List<Embebido>();
            foreach (var r in rows)
            {
                var vec = Parse(r.Emb);
                if (vec == null) continue;   // NULL, "[]" (sin texto) o inválido → no comparable

                double sumsq = 0;
                foreach (var v in vec) sumsq += (double)v * v;
                if (sumsq <= 0) continue;    // vector cero → no comparable

                list.Add(new Embebido
                {
                    Id = r.Id,
                    EmbEn = r.EmbEn,
                    TituloNorm = r.Titulo == null ? null : FirmaCalculator.Normalizar(r.Titulo),
                    Vec = vec,
                    Mag = Math.Sqrt(sumsq)
                });
            }
            return list;
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

        private static double Coseno(Embebido a, Embebido b)
        {
            if (a.Mag == 0 || b.Mag == 0) return 0;
            if (a.Vec.Length != b.Vec.Length) return 0;   // modelos/dims distintos → no comparable
            double dot = 0;
            var (va, vb) = (a.Vec, b.Vec);
            for (int k = 0; k < va.Length; k++) dot += (double)va[k] * vb[k];
            return dot / (a.Mag * b.Mag);
        }

        private static IEnumerable<List<int>> Particionar(List<int> src, int size)
        {
            for (int i = 0; i < src.Count; i += size)
                yield return src.GetRange(i, Math.Min(size, src.Count - i));
        }
    }
}
