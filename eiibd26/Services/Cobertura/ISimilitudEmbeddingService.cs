using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.Cobertura
{
    /// <summary>
    /// Motor de Cobertura — Fase 5. Calcula la similitud de COSENO entre embeddings densos
    /// (Voyage) y la persiste en <c>dbo.CoberturaSimilitudEmbedding</c>. Paralela a
    /// <see cref="ISimilitudService"/> (firma): misma orquestación (incremental, dirigida
    /// externo→propios + propios→propios) pero coseno denso y SIN los pre-filtros
    /// firma-específicos (Jaccard / riqueza / términos compartidos).
    /// </summary>
    public interface ISimilitudEmbeddingService
    {
        /// <summary>Corre el cálculo completo (incremental). Devuelve pares guardados/refrescados.</summary>
        Task<int> CalcularAsync(CancellationToken ct = default);

        /// <summary>Progreso: (comparaciones estimadas, pares guardados, último cálculo).</summary>
        Task<(long totalEstimado, int paresGuardados, DateTime? ultimo)> ObtenerProgresoAsync(CancellationToken ct = default);

        /// <summary>Borra todos los pares de la tabla de embeddings. Devuelve cuántos borró.</summary>
        Task<int> ResetearAsync(CancellationToken ct = default);

        /// <summary>Top pares por score (para inspección en el panel).</summary>
        Task<List<TopParDto>> ObtenerTopParesAsync(int limit = 50, CancellationToken ct = default);
    }
}
