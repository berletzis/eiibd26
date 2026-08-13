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
        /// Genera descripción educativa para un síntoma.
        /// </summary>
        /// <returns>
        /// <c>Reconocido = false</c> cuando el modelo declara que NO reconoce el término
        /// (guardrail anti-alucinación). En ese caso <c>Descripcion</c> viene vacía y el caller
        /// tiene PROHIBIDO persistirla: es un estado, no un texto que mostrar al paciente.
        /// </returns>
        Task<(string Descripcion, bool RelacionEII, bool Reconocido)> GenerarDescripcionSintomaAsync(string nombreSintoma, CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera descripción educativa para un tratamiento.
        /// </summary>
        /// <returns>
        /// <c>Reconocido = false</c> cuando el modelo declara que NO reconoce el término
        /// (guardrail anti-alucinación). Ver <see cref="GenerarDescripcionSintomaAsync"/>.
        /// </returns>
        Task<(string Descripcion, bool RelacionEII, string? NombreTraducido, bool Reconocido)> GenerarDescripcionTratamientoAsync(string nombreTratamiento, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triage de limpieza (eje 1): decide si el registro es una intervención terapéutica
        /// real (Válido), ruido heredado (Basura) o ambiguo (Dudoso → revisión humana).
        /// NO decide relación con EII: un procedimiento real sin relación con EII es Válido.
        /// Sesgo a conservar — ante la duda devuelve Dudoso, nunca Basura.
        /// </summary>
        /// <returns>
        /// Estado según <see cref="Models.TriageLimpieza"/>, confianza 0–1, motivo breve, y —
        /// si la IA la ofrece — el nivel de relación con EII y su razonamiento (eje 2).
        /// </returns>
        /// <remarks>
        /// A diferencia de los generadores de descripción, este método NO escribe las
        /// propiedades <c>Ultimo*</c>: devuelve todo en la tupla para no pisar el estado
        /// que el controller lee después de generar una descripción.
        /// </remarks>
        Task<(byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento)> ClasificarTratamientoAsync(
            string nombre,
            string? descripcionExistente,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Triage de limpieza (eje 1) para SÍNTOMAS: decide si el registro nombra una
        /// manifestación clínica real que un paciente siente y reporta (Válido), ruido o un
        /// no-síntoma mal capturado (Basura), o algo ambiguo (Dudoso → revisión humana).
        /// NO decide relación con EII: un síntoma real de otra condición es Válido.
        /// Sesgo a conservar — ante la duda devuelve Dudoso, nunca Basura.
        /// </summary>
        /// <returns>
        /// Estado según <see cref="Models.TriageLimpieza"/>, confianza 0–1, motivo breve, y —
        /// si la IA la ofrece — el nivel de relación con EII y su razonamiento (eje 2).
        /// </returns>
        /// <remarks>
        /// Igual que el de tratamientos, NO escribe las propiedades <c>Ultimo*</c>: devuelve
        /// todo en la tupla para no pisar el estado que el controller lee después de generar
        /// una descripción.
        /// </remarks>
        Task<(byte Estado, double Confianza, string Motivo, MedicalRelationType? Nivel, string? Razonamiento)> ClasificarSintomaAsync(
            string nombre,
            string? descripcionExistente,
            CancellationToken cancellationToken = default);

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
