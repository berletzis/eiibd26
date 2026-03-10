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
        /// <returns>Lista de términos agrupables por letra</returns>
        Task<List<GlossaryTermDto>> GetTermsByTypeAsync(GlossaryTermType tipo);

        /// <summary>
        /// Obtiene detalle completo de un término por slug
        /// </summary>
        /// <param name="slug">Slug del término (ej: "fatiga")</param>
        /// <returns>Detalle del término con definición médica y artículos relacionados</returns>
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
    }
}
