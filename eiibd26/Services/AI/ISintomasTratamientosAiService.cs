using eiibd26.Models;
using eiibd26.Models.Glossary;

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
        Task<(string Descripcion, bool RelacionEII)> GenerarDescripcionSintomaAsync(string nombreSintoma, CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera descripción educativa para un tratamiento
        /// </summary>
        Task<(string Descripcion, bool RelacionEII, string? NombreTraducido)> GenerarDescripcionTratamientoAsync(string nombreTratamiento, CancellationToken cancellationToken = default);

        /// <summary>Última explicación de relación con EII generada</summary>
        string UltimaExplicacionEII { get; }

        /// <summary>Últimas fuentes sugeridas por la IA</summary>
        string UltimasFuentes { get; }

        /// <summary>Último nombre traducido al español (para tratamientos)</summary>
        string? UltimoNombreTraducido { get; }

        /// <summary>
        /// Nivel de relación con EII sugerido por NINA (Directa / Indirecta / Secundaria).
        /// Null si no se pudo determinar o la relación es NO.
        /// </summary>
        MedicalRelationType? UltimoNivelRelacion { get; }

        /// <summary>
        /// Razonamiento clínico breve para mostrar en el glosario.
        /// Máximo ~200 caracteres.
        /// </summary>
        string? UltimoRazonamiento { get; }
    }
}
