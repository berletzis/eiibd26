using System.Threading;
using System.Threading.Tasks;
using eiibd26.Models.Contenidos;

namespace eiibd26.Services.Contenidos
{
    /// <summary>
    /// Proyección editorial del Motor de Cobertura hacia un backlog de oportunidades (F1).
    /// REUTILIZA <c>ICoberturaVistaService.ObtenerCoberturaTemasAsync</c> (misma fuente de
    /// verdad, sin recálculo) y le añade el estado editable de <c>OportunidadEstado</c>.
    /// No toca el motor ni el panel técnico de Cobertura/Similitud.
    /// </summary>
    public interface IOportunidadesService
    {
        /// <summary>
        /// Backlog de oportunidades de cobertura, dividido en las dos lentes:
        /// "Escribir nuevo" (huecos) y "Ampliar" (débiles). Vocabulario de editor, sin scores.
        /// </summary>
        /// <param name="fuente">Filtra por nombre de fuente (null = todas).</param>
        /// <param name="verDescartados">Si false, oculta los ítems en estado Descartado.</param>
        Task<OportunidadesVistaDto> ObtenerAsync(string? fuente, bool verDescartados, CancellationToken ct = default);

        /// <summary>Persiste (upsert) el estado del backlog de una oportunidad.</summary>
        Task ActualizarEstadoAsync(string tipo, int refId, EstadoBacklog estado, string? editorUserId, CancellationToken ct = default);
    }
}
