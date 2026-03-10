using eiibd26.Services.Glossary.DTOs;

namespace eiibd26.Services.Glossary.Adapters
{
    /// <summary>
    /// Adapter para leer datos médicos de forma desacoplada.
    /// ⚠️ SOLO LECTURA (READ ONLY) - No modifica datos médicos.
    /// Implementa el patrón Adapter para separar Glossary de Medical Domain.
    /// </summary>
    public interface IMedicalDataAdapter
    {
        /// <summary>
        /// Obtiene la definición médica de un síntoma
        /// </summary>
        /// <param name="sintomaId">ID del síntoma</param>
        /// <returns>Definición médica o null si no existe</returns>
        Task<MedicalDefinitionDto?> GetSymptomDefinitionAsync(int sintomaId);

        /// <summary>
        /// Obtiene la definición médica de un tratamiento
        /// </summary>
        /// <param name="tratamientoId">ID del tratamiento</param>
        /// <returns>Definición médica o null si no existe</returns>
        Task<MedicalDefinitionDto?> GetTreatmentDefinitionAsync(int tratamientoId);
    }
}
