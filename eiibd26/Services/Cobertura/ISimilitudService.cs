using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.Cobertura
{
    /// <summary>Par de mayor similitud, para la vista de inspección.</summary>
    public class TopParDto
    {
        public byte TipoPar { get; set; }          // 1=propio-propio, 2=propio-externo
        public int AId { get; set; }
        public string ATitulo { get; set; } = "";
        public int BId { get; set; }
        public string BEtiqueta { get; set; } = ""; // título (propio) o URL (externo)
        public decimal Score { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    /// <summary>
    /// Motor de Cobertura — Fase 3. Calcula la similitud (coseno con pre-filtro Jaccard)
    /// entre firmas, de forma DIRIGIDA: externo→propios y propios→propios (nunca externo-externo).
    /// </summary>
    public interface ISimilitudService
    {
        /// <summary>(comparaciones estimadas, pares guardados, último cálculo).</summary>
        Task<(long totalEstimado, int paresGuardados, DateTime? ultimo)> ObtenerProgresoAsync(CancellationToken ct = default);

        /// <summary>Calcula pares (incremental: salta los ya calculados cuyas firmas no cambiaron).
        /// Devuelve cuántos pares se guardaron/refrescaron.</summary>
        Task<int> CalcularAsync(CancellationToken ct = default);

        /// <summary>Borra todos los pares (recálculo desde cero).</summary>
        Task<int> ResetearAsync(CancellationToken ct = default);

        /// <summary>Top-N pares por score, con etiquetas resueltas.</summary>
        Task<List<TopParDto>> ObtenerTopParesAsync(int limit = 50, CancellationToken ct = default);
    }
}
