using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Firma;
using eiibd26.Models.Glossary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Cobertura
{
    /// <inheritdoc cref="IFirmaService"/>
    /// <remarks>
    /// El acceso a datos vive aquí (leer glosario, leer/guardar firmas). El cálculo puro
    /// (normalizar, contar, serializar) está en la biblioteca compartida <c>eiibd26.Firma</c>
    /// (<see cref="FirmaCalculator"/>), la misma que usa el Worker para firmar externos, para
    /// que ambas firmas sean idénticas en método.
    /// </remarks>
    public class FirmaService : IFirmaService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FirmaService> _logger;

        // Publicado + Destacado + Archivado (Domain.Constants.EstadoPublicacion.Visibles).
        private static readonly int[] EstadosVisibles = { 1, 2, 3 };

        // Lotes chicos: los cuerpos HTML pueden ser grandes (imágenes base64, etc.).
        private const int BatchSize = 25;

        public FirmaService(ApplicationDbContext db, ILogger<FirmaService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> FirmarPendientesAsync(CancellationToken ct = default)
        {
            // Vocabulario cargado UNA vez por corrida (cache en memoria local).
            var vocab = await CargarVocabularioAsync(ct);
            if (vocab.Count == 0)
            {
                _logger.LogWarning("[Firma] Vocabulario EII vacío (0 términos Directa activos). Nada que firmar.");
                return 0;
            }

            _logger.LogInformation("[Firma] Vocabulario cargado: {Count} términos EII (relación Directa).", vocab.Count);

            int firmados = 0;

            // Reanudable: cada lote re-consulta Firma == null. Los recién firmados (ya guardados)
            // salen del filtro, así que el while converge sin repetir ni entrar en bucle infinito.
            while (!ct.IsCancellationRequested)
            {
                var lote = await _db.Contenidos
                    .Where(c => !c.Eliminado
                                && c.EstadoPublicacion.HasValue
                                && EstadosVisibles.Contains(c.EstadoPublicacion.Value)
                                && c.Firma == null)
                    .OrderBy(c => c.Id)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (lote.Count == 0) break;

                foreach (var c in lote)
                {
                    c.Firma = FirmaCalculator.Calcular(c.ContenidoTitulo, c.ContenidoTextoL, vocab);
                    c.FirmaCalculadaEn = DateTime.Now;
                    firmados++;
                }

                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation("[Firma] Corrida completa: {Count} contenidos firmados.", firmados);
            return firmados;
        }

        public async Task<(int total, int firmados)> ObtenerProgresoAsync(CancellationToken ct = default)
        {
            var baseQuery = _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value));

            var total = await baseQuery.CountAsync(ct);
            var firmados = await baseQuery.CountAsync(c => c.Firma != null, ct);
            return (total, firmados);
        }

        public async Task<int> ResetearFirmasAsync(CancellationToken ct = default)
        {
            var afectados = await _db.Contenidos
                .Where(c => !c.Eliminado
                            && c.EstadoPublicacion.HasValue
                            && EstadosVisibles.Contains(c.EstadoPublicacion.Value))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Firma, (string?)null)
                    .SetProperty(c => c.FirmaCalculadaEn, (DateTime?)null), ct);

            _logger.LogInformation("[Firma] Reset total: {Count} firmas puestas a NULL.", afectados);
            return afectados;
        }

        /// <summary>
        /// Vocabulario = GlossaryTerm.Nombre con Activo=1 y relación EII DIRECTA
        /// (MedicalRelationSuggestedId = Directa), SIN filtrar por TipoTermino (incluye
        /// síntomas, tratamientos y conceptos generales EII). El cálculo (normalizar +
        /// compilar regex) lo hace la biblioteca compartida.
        /// </summary>
        private async Task<IReadOnlyList<VocabularioTermino>> CargarVocabularioAsync(CancellationToken ct)
        {
            var nombres = await _db.GlossaryTerms
                .AsNoTracking()
                .Where(t => t.Activo && t.MedicalRelationSuggestedId == MedicalRelationType.Directa)
                .Select(t => t.Nombre)
                .ToListAsync(ct);

            return FirmaCalculator.CompilarVocabulario(nombres);
        }
    }
}
