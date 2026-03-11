using eiibd26.Models.Glossary;
using eiibd26.Services.Glossary.DTOs;

namespace eiibd26.Services.Glossary
{
    /// <summary>
    /// Servicio principal del módulo Glosario.
    /// API pública para navegación médica y descubrimiento de contenido.
    /// </summary>
    public interface IGlossaryService
    {
        /// <summary>
        /// Obtiene datos para la página de inicio del glosario
        /// </summary>
        Task<GlossaryHomeDto> GetGlossaryHomeAsync();

        /// <summary>
        /// Obtiene lista de términos por tipo (para índice A-Z)
        /// </summary>
        /// <param name="tipo">Tipo de término (Sintoma o Tratamiento)</param>
        Task<List<GlossaryTermDto>> GetTermsByTypeAsync(GlossaryTermType tipo);

        /// <summary>
        /// Obtiene detalle completo de un término por slug
        /// </summary>
        /// <param name="slug">Slug del término (ej: "fatiga")</param>
        Task<GlossaryTermDetailDto?> GetTermBySlugAsync(string slug);

        /// <summary>
        /// Busca términos por nombre (para buscador)
        /// </summary>
        /// <param name="query">Texto de búsqueda</param>
        /// <param name="maxResults">Máximo de resultados</param>
        Task<List<GlossaryTermDto>> SearchTermsAsync(string query, int maxResults = 20);

        /// <summary>
        /// Obtiene artículos CMS relacionados con un término
        /// </summary>
        /// <param name="termName">Nombre del término</param>
        /// <param name="maxResults">Máximo de resultados</param>
        Task<List<RelatedContentDto>> GetRelatedContentsAsync(string termName, int maxResults = 10);

        // ===== VALIDACIÓN HUMANA =====

        /// <summary>
        /// Registra una validación humana sobre un término.
        /// Un usuario solo puede votar una vez por tipo/nivel.
        /// </summary>
        /// <param name="termId">ID del término</param>
        /// <param name="userId">ID del usuario validador</param>
        /// <param name="validationType">Tipo de validación</param>
        /// <param name="relationTypeId">Nivel de relación (solo para RelationValidation)</param>
        /// <param name="comment">Comentario clínico opcional</param>
        /// <returns>True si se registró exitosamente; false si ya existía esa validación</returns>
        Task<bool> AddValidationAsync(
            int termId,
            string userId,
            GlossaryValidationType validationType,
            MedicalRelationType? relationTypeId,
            string? comment);

        /// <summary>
        /// Obtiene los conteos de badges de confianza para un término
        /// </summary>
        Task<GlossaryValidationCountsDto> GetValidationCountsAsync(int termId);
    }
}
