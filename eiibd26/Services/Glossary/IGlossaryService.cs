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
        /// Ids de términos ACTIVOS cuyo registro médico vinculado está en triage Dudoso
        /// (RevisionLimpiezaEstado = 3). Para el chip "Dudosos" del glosario, que SOLO se
        /// muestra a curadores (Administrador/Medico). Consulta liviana (solo ids) que las
        /// vistas disparan únicamente cuando el usuario tiene ese rol.
        /// </summary>
        Task<HashSet<int>> GetDudosoTermIdsAsync(GlossaryTermType tipo);

        /// <summary>
        /// Propaga el borrado lógico de tratamientos al glosario:
        /// tratamiento eliminado ⇒ término inactivo; restaurado ⇒ término activo.
        /// Invariante que mantiene alineados los conteos del home (tabla <c>tratamientos</c>)
        /// con los del glosario (<c>GlossaryTerm.Activo</c>).
        /// </summary>
        /// <param name="tratamientoIds">Ids de tratamientos cuyo término hay que sincronizar</param>
        /// <param name="activo">Estado destino del término (<c>false</c> al eliminar, <c>true</c> al restaurar)</param>
        /// <returns>Número de términos efectivamente modificados</returns>
        Task<int> SincronizarActivoPorTratamientosAsync(
            IReadOnlyCollection<int> tratamientoIds,
            bool activo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Propaga el borrado lógico de síntomas al glosario:
        /// síntoma eliminado ⇒ término inactivo; restaurado ⇒ término activo.
        /// Invariante que mantiene alineados los conteos del home (tabla <c>sintomas</c>)
        /// con los del glosario (<c>GlossaryTerm.Activo</c>).
        /// </summary>
        /// <param name="sintomaIds">Ids de síntomas cuyo término hay que sincronizar</param>
        /// <param name="activo">Estado destino del término (<c>false</c> al eliminar, <c>true</c> al restaurar)</param>
        /// <returns>Número de términos efectivamente modificados</returns>
        Task<int> SincronizarActivoPorSintomasAsync(
            IReadOnlyCollection<int> sintomaIds,
            bool activo,
            CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Lista las validaciones de RELACIÓN (nivel Directa/Indirecta/Secundaria) que hizo
        /// un usuario, con el término ya resuelto (nombre, slug, tipo). Ordenadas de la más
        /// reciente a la más antigua. Incluye las aún no aprobadas — es el historial del propio
        /// profesional, así que ve también lo que sigue en revisión.
        /// </summary>
        /// <param name="userId">Id del usuario validador</param>
        Task<List<GlossaryRelationValidationDto>> ObtenerValidacionesRelacionMedicoAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene los top términos filtrados por calidad (validación humana y nivel actualizado).
        /// Devuelve una lista de resúmenes (Id, Nombre, Slug, ShortDescription, LastHumanUpdateDate, Views, Badges).
        /// </summary>
        Task<List<GlossaryTermSummaryDto>> GetTopTermsByQualityAsync(GlossaryTermType type, int limit = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene preguntas relacionadas (top N) asociadas a un síntoma o tratamiento.
        /// </summary>
        Task<List<RelatedQuestionDto>> GetRelatedQuestionsAsync(int? symptomId, int? treatmentId, int maxResults = 5);
    }
}
