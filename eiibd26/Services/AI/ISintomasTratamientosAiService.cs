using eiibd26.Models;

namespace eiibd26.Services.AI
{
    /// <summary>
    /// Servicio especializado para generar descripciones de síntomas y tratamientos usando IA
    /// </summary>
    public interface ISintomasTratamientosAiService
    {
        /// <summary>
        /// Genera descripción educativa para un síntoma
        /// </summary>
        /// <param name="nombreSintoma">Nombre del síntoma</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Descripción generada y si tiene relación con EII</returns>
        Task<(string Descripcion, bool RelacionEII)> GenerarDescripcionSintomaAsync(string nombreSintoma, CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera descripción educativa para un tratamiento
        /// </summary>
        /// <param name="nombreTratamiento">Nombre del tratamiento</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Descripción generada, si tiene relación con EII, y nombre traducido al español (si aplica)</returns>
        Task<(string Descripcion, bool RelacionEII, string? NombreTraducido)> GenerarDescripcionTratamientoAsync(string nombreTratamiento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la última explicación de relación con EII generada
        /// </summary>
        string UltimaExplicacionEII { get; }

        /// <summary>
        /// Obtiene las últimas fuentes sugeridas por la IA
        /// </summary>
        string UltimasFuentes { get; }

        /// <summary>
        /// Obtiene el último nombre traducido al español (para tratamientos)
        /// </summary>
        string? UltimoNombreTraducido { get; }
    }
}
