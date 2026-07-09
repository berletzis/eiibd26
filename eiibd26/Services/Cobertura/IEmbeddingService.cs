using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.Cobertura
{
    /// <summary>
    /// Motor de Cobertura — Fase 5 (embeddings). Genera el embedding semántico denso (Voyage)
    /// de los artículos propios publicados y lo persiste en <c>contenidos.Embedding</c>.
    /// Complementa a la firma (conteo de vocabulario): el embedding captura el significado para
    /// la similitud fina "más como este". Solo contenido propio (los externos los embebe el Worker).
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Embebe todos los publicados pendientes (Embedding IS NULL). Reanudable e idempotente:
        /// avanza por cursor de Id, así una corrida termina siempre (los lotes que fallen en Voyage
        /// quedan NULL y se reintentan en la próxima corrida, sin bucle infinito).
        /// Devuelve cuántos contenidos embebió en esta corrida.
        /// </summary>
        Task<int> EmbedPendientesAsync(CancellationToken ct = default);

        /// <summary>Progreso actual: (total de publicados, cuántos ya tienen embedding).</summary>
        Task<(int total, int embebidos)> ObtenerProgresoAsync(CancellationToken ct = default);

        /// <summary>
        /// Pone Embedding/EmbeddingModelo/EmbeddingCalculadoEn = NULL en todos los publicados
        /// (recálculo desde cero, p. ej. si cambia el modelo). Devuelve cuántas filas reseteó.
        /// </summary>
        Task<int> ResetearEmbeddingsAsync(CancellationToken ct = default);
    }
}
