using eiibd26.Models.Glossary;
using eiibd26.Services.Community;
using eiibd26.Services.Glossary.Adapters;
using eiibd26.Services.Glossary.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Services.Glossary
{
    /// <summary>
    /// Implementación del servicio de glosario médico.
    /// Orquesta lectura desde dominios médico y CMS sin acoplar.
    /// </summary>
    public class GlossaryService : IGlossaryService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMedicalDataAdapter _medicalAdapter;
        private readonly ICommunityExperienceService _community;
        private readonly ILogger<GlossaryService> _logger;

        public GlossaryService(
            ApplicationDbContext db,
            IMedicalDataAdapter medicalAdapter,
            ICommunityExperienceService community,
            ILogger<GlossaryService> logger)
        {
            _db             = db;
            _medicalAdapter = medicalAdapter;
            _community      = community;
            _logger         = logger;
        }

        /// <summary>
        /// Datos para página de inicio del glosario
        /// </summary>
        public async Task<GlossaryHomeDto> GetGlossaryHomeAsync()
        {
            try
            {
                var totalSintomas = await _db.GlossaryTerms
                    .CountAsync(gt => gt.TipoTermino == GlossaryTermType.Sintoma && gt.Activo);

                var totalTratamientos = await _db.GlossaryTerms
                    .CountAsync(gt => gt.TipoTermino == GlossaryTermType.Tratamiento && gt.Activo);

                return new GlossaryHomeDto
                {
                    TotalSintomas = totalSintomas,
                    TotalTratamientos = totalTratamientos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos del home del glosario");
                return new GlossaryHomeDto();
            }
        }

        /// <summary>
        /// Lista de términos por tipo (para índice A-Z)
        /// </summary>
        public async Task<List<GlossaryTermDto>> GetTermsByTypeAsync(GlossaryTermType tipo)
        {
            try
            {
                var terms = await _db.GlossaryTerms
                    .AsNoTracking()
                    .Where(gt => gt.TipoTermino == tipo && gt.Activo)
                    .OrderBy(gt => gt.Nombre)
                    .Select(gt => new GlossaryTermDto
                    {
                        Id = gt.Id,
                        Nombre = gt.Nombre,
                        Slug = gt.Slug,
                        TipoTermino = gt.TipoTermino
                    })
                    .ToListAsync();

                _logger.LogInformation("Obtenidos {Count} términos de tipo {Tipo}", terms.Count, tipo);

                return terms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener términos de tipo {Tipo}", tipo);
                return new List<GlossaryTermDto>();
            }
        }

        /// <summary>
        /// Detalle completo de un término (con definición médica + artículos)
        /// </summary>
        public async Task<GlossaryTermDetailDto?> GetTermBySlugAsync(string slug)
        {
            try
            {
                // 1. Proyección explícita: solo las columnas que necesitamos.
                //    Evita errores si alguna columna nueva aún no existe en el esquema.
                var term = await _db.GlossaryTerms
                    .AsNoTracking()
                    .Where(gt => gt.Slug == slug && gt.Activo)
                    .Select(gt => new
                    {
                        gt.Id,
                        gt.Nombre,
                        gt.Slug,
                        gt.TipoTermino,
                        MedicalLinkSintomaId     = gt.MedicalLink != null ? gt.MedicalLink.SintomaId     : null,
                        MedicalLinkTratamientoId = gt.MedicalLink != null ? gt.MedicalLink.TratamientoId : null
                    })
                    .FirstOrDefaultAsync();

                if (term == null)
                {
                    _logger.LogWarning("Término con slug '{Slug}' no encontrado", slug);
                    return null;
                }

                var detail = new GlossaryTermDetailDto
                {
                    Id               = term.Id,
                    Nombre           = term.Nombre,
                    Slug             = term.Slug,
                    TipoTermino      = term.TipoTermino,
                    SintomaId        = term.MedicalLinkSintomaId,
                    TratamientoId    = term.MedicalLinkTratamientoId,
                    ValidationCounts = await GetValidationCountsAsync(term.Id)
                };

                // 2. Leer definición médica a través del adapter (desacoplado)
                if (term.MedicalLinkSintomaId.HasValue)
                {
                    detail.DefinicionMedica = await _medicalAdapter.GetSymptomDefinitionAsync(
                        term.MedicalLinkSintomaId.Value);
                }
                else if (term.MedicalLinkTratamientoId.HasValue)
                {
                    detail.DefinicionMedica = await _medicalAdapter.GetTreatmentDefinitionAsync(
                        term.MedicalLinkTratamientoId.Value);
                }

                // 3. Buscar artículos relacionados del CMS
                detail.ArticulosRelacionados = await GetRelatedContentsAsync(term.Nombre, 10);

                // 4. Experiencias de la comunidad (READ-ONLY)
                if (term.MedicalLinkSintomaId.HasValue)
                    detail.ExperienciasComunidad = await _community.GetRecentExperiencesBySymptomAsync(term.MedicalLinkSintomaId.Value);
                else if (term.MedicalLinkTratamientoId.HasValue)
                    detail.ExperienciasComunidad = await _community.GetRecentExperiencesByTreatmentAsync(term.MedicalLinkTratamientoId.Value);

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalle del término '{Slug}'", slug);
                return null;
            }
        }

        /// <summary>
        /// Búsqueda de términos (para buscador)
        /// </summary>
        public async Task<List<GlossaryTermDto>> SearchTermsAsync(string query, int maxResults = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<GlossaryTermDto>();

            try
            {
                var terms = await _db.GlossaryTerms
                    .AsNoTracking()
                    .Where(gt => gt.Activo && gt.Nombre.Contains(query))
                    .OrderBy(gt => gt.Nombre)
                    .Take(maxResults)
                    .Select(gt => new GlossaryTermDto
                    {
                        Id = gt.Id,
                        Nombre = gt.Nombre,
                        Slug = gt.Slug,
                        TipoTermino = gt.TipoTermino
                    })
                    .ToListAsync();

                _logger.LogInformation("Búsqueda '{Query}' encontró {Count} resultados", query, terms.Count);

                return terms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar términos con query '{Query}'", query);
                return new List<GlossaryTermDto>();
            }
        }

        /// <summary>
        /// Registra una validación humana sobre un término.
        /// Impide duplicados por usuario/tipo/nivel mediante índice único.
        /// </summary>
        public async Task<bool> AddValidationAsync(
            int termId,
            string userId,
            GlossaryValidationType validationType,
            MedicalRelationType? relationTypeId,
            string? comment)
        {
            ArgumentNullException.ThrowIfNull(userId);

            try
            {
                // Evitar duplicado: mismo usuario, mismo tipo y mismo nivel
                var exists = await _db.GlossaryValidations.AnyAsync(v =>
                    v.GlossaryTermId == termId
                    && v.UserId == userId
                    && v.ValidationType == validationType
                    && v.MedicalRelationTypeId == relationTypeId);

                if (exists)
                {
                    _logger.LogInformation(
                        "Usuario {UserId} ya validó el término {TermId} con tipo {Type}/nivel {Level}",
                        userId, termId, validationType, relationTypeId);
                    return false;
                }

                _db.GlossaryValidations.Add(new GlossaryValidation
                {
                    GlossaryTermId = termId,
                    UserId = userId,
                    ValidationType = validationType,
                    MedicalRelationTypeId = relationTypeId,
                    Approved = true,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                _logger.LogInformation(
                    "Validación registrada: término {TermId}, usuario {UserId}, tipo {Type}",
                    termId, userId, validationType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar validación del término {TermId}", termId);
                return false;
            }
        }

        /// <summary>
        /// Calcula conteos de badges de confianza para un término,
        /// incluyendo comentarios clínicos y razonamiento de IA.
        /// </summary>
        public async Task<GlossaryValidationCountsDto> GetValidationCountsAsync(int termId)
        {
            try
            {
                // Campos base (CreatedByAI existe desde el principio)
                var term = await _db.GlossaryTerms
                    .AsNoTracking()
                    .Where(t => t.Id == termId)
                    .Select(t => new { t.CreatedByAI, t.MedicalRelationSuggestedId })
                    .FirstOrDefaultAsync();

                // AiReasoning es columna nueva — cargar por separado para no romper si no existe aún
                string? aiReasoning = null;
                try
                {
                    aiReasoning = await _db.GlossaryTerms
                        .AsNoTracking()
                        .Where(t => t.Id == termId)
                        .Select(t => t.AiReasoning)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Columna AiReasoning no disponible aún para término {TermId}", termId);
                }

                var meaningCount = await _db.GlossaryValidations
                    .CountAsync(v =>
                        v.GlossaryTermId == termId
                        && v.ValidationType == GlossaryValidationType.MeaningValidation
                        && v.Approved);

                // Agrupar validaciones humanas de relación con sus comentarios
                var rawGroups = await _db.GlossaryValidations
                    .Where(v =>
                        v.GlossaryTermId == termId
                        && v.ValidationType == GlossaryValidationType.RelationValidation
                        && v.Approved
                        && v.MedicalRelationTypeId != null)
                    .Select(v => new { v.MedicalRelationTypeId, v.Comment })
                    .ToListAsync();

                var relationGroups = rawGroups
                    .GroupBy(v => v.MedicalRelationTypeId!.Value)
                    .Select(g => new
                    {
                        RelationType = g.Key,
                        HumanCount   = g.Count(),
                        Comments     = g.Where(v => !string.IsNullOrWhiteSpace(v.Comment))
                                        .Select(v => v.Comment!)
                                        .ToList()
                    })
                    .ToList();

                // Si NINA ya sugirió un nivel, suma +1 a ese nivel (su voto cuenta)
                var aiSuggested = term?.MedicalRelationSuggestedId;
                var allLevels = relationGroups
                    .Select(g => g.RelationType)
                    .Union(aiSuggested.HasValue ? new[] { aiSuggested.Value } : Array.Empty<MedicalRelationType>())
                    .Distinct();

                var countedGroups = allLevels
                    .Select(level =>
                    {
                        var human = relationGroups.FirstOrDefault(g => g.RelationType == level);
                        int aiVote = aiSuggested.HasValue && aiSuggested.Value == level ? 1 : 0;
                        return new
                        {
                            RelationType = level,
                            Count        = (human?.HumanCount ?? 0) + aiVote,
                            Comments     = human?.Comments ?? new List<string>()
                        };
                    })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var maxCount = countedGroups.FirstOrDefault()?.Count ?? 0;

                var consensus = countedGroups
                    .Select(g => new RelationConsensusItemDto
                    {
                        RelationType   = g.RelationType,
                        Count          = g.Count,
                        IsTopConsensus = g.Count == maxCount && maxCount > 0,
                        Comments       = g.Comments
                    })
                    .ToList();

                return new GlossaryValidationCountsDto
                {
                    CreatedByAI            = term?.CreatedByAI ?? true,
                    MeaningValidationCount = meaningCount,
                    RelationSuggested      = term?.MedicalRelationSuggestedId,
                    AiReasoning            = aiReasoning,
                    RelationConsensus      = consensus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener conteos de validación del término {TermId}", termId);
                return new GlossaryValidationCountsDto();
            }
        }

        /// <summary>
        /// Artículos CMS relacionados con un término (READ ONLY del CMS)
        /// </summary>
        public async Task<List<RelatedContentDto>> GetRelatedContentsAsync(string termName, int maxResults = 10)
        {
            if (string.IsNullOrWhiteSpace(termName))
                return new List<RelatedContentDto>();

            try
            {
                // ⚠️ READ ONLY del dominio CMS
                var contenidos = await _db.Contenidos
                    .AsNoTracking()
                    .Where(c => !c.Eliminado 
                        && c.EstadoPublicacion == 1 // 1 = Publicado (ajustar según tu modelo)
                        && (c.ContenidoTitulo.Contains(termName) || c.ContenidoTextoC.Contains(termName)))
                    .OrderByDescending(c => c.ContenidoFechaInicio)
                    .Take(maxResults)
                    .Select(c => new RelatedContentDto
                    {
                        Id = c.Id,
                        Titulo = c.ContenidoTitulo ?? "",
                        Slug = c.ContenidoTituloSlug ?? "",
                        Resumen = c.ContenidoTextoC != null ? c.ContenidoTextoC.Substring(0, Math.Min(200, c.ContenidoTextoC.Length)) : null,
                        ImagenDestacada = c.URLImagenPrincipal,
                        FechaPublicacion = c.ContenidoFechaInicio
                    })
                    .ToListAsync();

                _logger.LogInformation("Encontrados {Count} artículos relacionados con '{TermName}'", 
                    contenidos.Count, termName);

                return contenidos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener contenidos relacionados con '{TermName}'", termName);
                return new List<RelatedContentDto>();
            }
        }
    }
}
