using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Firma;
using eiibd26.Models;
using eiibd26.Voyage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Cobertura
{
    /// <inheritdoc cref="IEmbeddingService"/>
    /// <remarks>
    /// El texto a embeber es título + cuerpo, con el HTML quitado (reusa
    /// <see cref="FirmaCalculator.StripHtml"/>) pero SIN normalizar (se preservan acentos,
    /// mayúsculas y signos: el modelo multilingüe usa el significado natural). El vector se
    /// serializa como JSON de floats. El cálculo de similitud es de otra fase (SimilitudEmbedding).
    /// </remarks>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly ApplicationDbContext _db;
        private readonly IVoyageEmbeddingClient _voyage;
        private readonly ILogger<EmbeddingService> _logger;

        // Publicado + Destacado + Archivado (mismo filtro que la firma).
        private static readonly int[] EstadosVisibles = { 1, 2, 3 };

        // Lote de contenidos por request a Voyage. Modesto para no exceder el tope de tokens/request.
        private const int BatchSize = 16;
        // Tope de caracteres del texto a embeber (voyage-4-large admite 32K tokens; esto es holgado).
        private const int MaxChars = 32000;
        // Pausa entre lotes (defensiva ante rate limits; con tier de pago es despreciable).
        private const int PaceMs = 200;
        // Marcador de "sin texto embebible" (páginas institucionales): se salta en la similitud.
        private const string SinTextoModelo = "(sin-texto)";
        private const string VectorVacio = "[]";

        private static readonly Regex EspaciosRegex = new(@"\s+", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOpts = new();

        public EmbeddingService(ApplicationDbContext db, IVoyageEmbeddingClient voyage, ILogger<EmbeddingService> logger)
        {
            _db = db;
            _voyage = voyage;
            _logger = logger;
        }

        public async Task<int> EmbedPendientesAsync(CancellationToken ct = default)
        {
            if (!_voyage.Habilitado)
            {
                _logger.LogWarning("[Embedding] Voyage sin API key configurada. Nada que embeber.");
                return 0;
            }

            int embebidos = 0;
            int cursor = 0;   // avanza siempre → la corrida termina aunque un lote falle.

            while (!ct.IsCancellationRequested)
            {
                var lote = await _db.Contenidos
                    .Where(c => !c.Eliminado
                                && c.EstadoPublicacion.HasValue
                                && EstadosVisibles.Contains(c.EstadoPublicacion.Value)
                                && c.Embedding == null
                                && c.Id > cursor)
                    .OrderBy(c => c.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (lote.Count == 0) break;
                cursor = lote[^1].Id;

                var ahora = DateTime.Now;
                var conTexto = new List<(Contenido c, string texto)>();

                foreach (var c in lote)
                {
                    var texto = ConstruirTexto(c.ContenidoTitulo, c.ContenidoTextoL);
                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        // Sin texto embebible (institucional): marcar procesado con vector vacío
                        // para que la corrida "complete" y la similitud lo salte.
                        c.Embedding = VectorVacio;
                        c.EmbeddingModelo = SinTextoModelo;
                        c.EmbeddingCalculadoEn = ahora;
                    }
                    else
                    {
                        conTexto.Add((c, texto));
                    }
                }

                if (conTexto.Count > 0)
                {
                    var batch = await _voyage.EmbedAsync(conTexto.Select(x => x.texto).ToList(), ct);
                    if (batch == null || batch.Vectores.Count != conTexto.Count)
                    {
                        _logger.LogWarning(
                            "[Embedding] Lote falló en Voyage (Ids {A}–{B}). Quedan NULL y se reintentan en la próxima corrida.",
                            lote[0].Id, lote[^1].Id);
                        // Persistir solo los 'sin texto' ya marcados; los conTexto siguen NULL.
                        await _db.SaveChangesAsync(ct);
                        _db.ChangeTracker.Clear();
                        if (PaceMs > 0) await Task.Delay(PaceMs, ct);
                        continue;
                    }

                    for (int i = 0; i < conTexto.Count; i++)
                    {
                        conTexto[i].c.Embedding = JsonSerializer.Serialize(batch.Vectores[i], JsonOpts);
                        conTexto[i].c.EmbeddingModelo = _voyage.Modelo;
                        conTexto[i].c.EmbeddingCalculadoEn = ahora;
                        embebidos++;
                    }
                }

                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                if (PaceMs > 0) await Task.Delay(PaceMs, ct);
            }

            _logger.LogInformation("[Embedding] Corrida completa: {Count} contenidos embebidos.", embebidos);
            return embebidos;
        }

        public async Task<(int total, int embebidos)> ObtenerProgresoAsync(CancellationToken ct = default)
        {
            var baseQuery = _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value));

            var total = await baseQuery.CountAsync(ct);
            var embebidos = await baseQuery.CountAsync(c => c.Embedding != null, ct);
            return (total, embebidos);
        }

        public async Task<int> ResetearEmbeddingsAsync(CancellationToken ct = default)
        {
            var afectados = await _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Embedding, (string?)null)
                    .SetProperty(c => c.EmbeddingModelo, (string?)null)
                    .SetProperty(c => c.EmbeddingCalculadoEn, (DateTime?)null), ct);

            _logger.LogInformation("[Embedding] Reset total: {Count} embeddings puestos a NULL.", afectados);
            return afectados;
        }

        /// <summary>título + cuerpo, sin HTML (pero con acentos/mayúsculas), colapsado y acotado.</summary>
        private static string ConstruirTexto(string? titulo, string? cuerpo)
        {
            var plano = WebUtility.HtmlDecode(FirmaCalculator.StripHtml($"{titulo}. {cuerpo}"));
            plano = EspaciosRegex.Replace(plano, " ").Trim();
            if (plano.Length > MaxChars) plano = plano.Substring(0, MaxChars);
            return plano;
        }
    }
}
