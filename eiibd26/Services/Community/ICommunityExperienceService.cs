using eiibd26.Services.Glossary.DTOs;

namespace eiibd26.Services.Community
{
    /// <summary>
    /// Integración de lectura de experiencias comunitarias para el módulo Glosario.
    /// READ-ONLY — no modifica ningún modelo existente.
    /// </summary>
    public interface ICommunityExperienceService
    {
        /// <summary>
        /// Obtiene las experiencias más recientes vinculadas a un síntoma.
        /// </summary>
        Task<List<CommunityExperienceDto>> GetRecentExperiencesBySymptomAsync(
            int symptomId,
            int maxResults = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene las experiencias más recientes vinculadas a un tratamiento.
        /// </summary>
        Task<List<CommunityExperienceDto>> GetRecentExperiencesByTreatmentAsync(
            int treatmentId,
            int maxResults = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Indica si existen experiencias para un síntoma dado.
        /// </summary>
        Task<bool> HasExperiencesBySymptomAsync(int symptomId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indica si existen experiencias para un tratamiento dado.
        /// </summary>
        Task<bool> HasExperiencesByTreatmentAsync(int treatmentId, CancellationToken cancellationToken = default);
    }
}
