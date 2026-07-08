using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.Cobertura
{
    /// <summary>
    /// Motor de Cobertura — Fase 1. Calcula la "firma" de cobertura EII de los
    /// artículos propios publicados (representación numérica del contenido sobre
    /// el vocabulario EII del glosario) y la persiste en <c>contenidos.Firma</c>.
    /// Solo contenido propio; sin externos, traducción ni similitud (fases posteriores).
    /// </summary>
    public interface IFirmaService
    {
        /// <summary>
        /// Firma todos los publicados pendientes (Firma IS NULL). Reanudable e idempotente:
        /// re-consulta los pendientes por lote, así que reanudar retoma solo lo que falta.
        /// Devuelve cuántos contenidos firmó en esta corrida.
        /// </summary>
        Task<int> FirmarPendientesAsync(CancellationToken ct = default);

        /// <summary>Progreso actual: (total de publicados, cuántos ya tienen firma).</summary>
        Task<(int total, int firmados)> ObtenerProgresoAsync(CancellationToken ct = default);

        /// <summary>
        /// Pone Firma/FirmaCalculadaEn = NULL en todos los publicados (recálculo desde cero,
        /// p. ej. si cambia el vocabulario). Devuelve cuántas filas reseteó.
        /// </summary>
        Task<int> ResetearFirmasAsync(CancellationToken ct = default);
    }
}
