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
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
        private readonly ILogger<GlossaryService> _logger;

        public GlossaryService(
            ApplicationDbContext db,
            IMedicalDataAdapter medicalAdapter,
            ICommunityExperienceService community,
            ILogger<GlossaryService> logger,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _db             = db;
            _medicalAdapter = medicalAdapter;
            _community      = community;
            _logger         = logger;
            _cache          = cache;
            _env            = env;
        }

        /// <summary>
        /// Datos para página de inicio del glosario
        /// </summary>
        public async Task<GlossaryHomeDto> GetGlossaryHomeAsync()
        {
            // AUDITORÍA: Antes de modificar
            // - Handler: Pages/Glosario/Index.OnGetAsync
            // - ViewModel: GlossaryHomeDto (TotalSintomas, TotalTratamientos)
            // Observación: Contaje debe provenir de las tablas reales de síntomas/tratamientos
            // aplicando filtros de soft-delete (Eliminado) y no depender de colecciones parciales.
            try
            {
                // Contar desde las tablas reales aplicando filtros lógicos (Excluir eliminados)
                var totalSintomas = await _db.sintomas
                    .AsNoTracking()
                    .CountAsync(s => !s.Eliminado);

                var totalTratamientos = await _db.tratamientos
                    .AsNoTracking()
                    .CountAsync(t => !t.Eliminado);

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

                // Calcular la cantidad total de usuarios relacionados (sintomasUsuario / tratamientoUsuario)
                try
                {
                    int relatedCount = 0;
                    if (detail.SintomaId.HasValue)
                    {
                        relatedCount = await _db.sintomasUsuario
                            .AsNoTracking()
                            .CountAsync(su => su.idSintoma == detail.SintomaId && !su.Eliminado);
                    }
                    else if (detail.TratamientoId.HasValue)
                    {
                        relatedCount = await _db.tratamientoUsuario
                            .AsNoTracking()
                            .CountAsync(tu => tu.idTratamiento == detail.TratamientoId && !tu.Eliminado);
                    }

                    detail.RelatedUsersCount = relatedCount;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo calcular RelatedUsersCount para término {TermId}", term.Id);
                    detail.RelatedUsersCount = 0;
                }

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
                // Cargar preguntas relacionadas (top 5)
                if (term.MedicalLinkSintomaId.HasValue)
                {
                    detail.PreguntasRelacionadas = await GetRelatedQuestionsAsync(term.MedicalLinkSintomaId.Value, null, 5);
                }
                else if (term.MedicalLinkTratamientoId.HasValue)
                {
                    detail.PreguntasRelacionadas = await GetRelatedQuestionsAsync(null, term.MedicalLinkTratamientoId.Value, 5);
                }

                // 4. Experiencias de la comunidad (READ-ONLY)
                if (term.MedicalLinkSintomaId.HasValue)
                    detail.ExperienciasComunidad = await _community.GetRecentExperiencesBySymptomAsync(term.MedicalLinkSintomaId.Value);
                else if (term.MedicalLinkTratamientoId.HasValue)
                    detail.ExperienciasComunidad = await _community.GetRecentExperiencesByTreatmentAsync(term.MedicalLinkTratamientoId.Value);

                // 5. Top terms by quality are not loaded here (index page uses separate endpoint)

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
                // Invalidate top lists cache so UI reflects new human validations immediately
                try
                {
                    var keys = new[]
                    {
                        $"glossary:top:{GlossaryTermType.Sintoma}:20",
                        $"glossary:top:{GlossaryTermType.Tratamiento}:20"
                    };
                    foreach (var k in keys)
                        _cache.Remove(k);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo invalidar cache de top glossary lists");
                }

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

        public async Task<List<GlossaryTermSummaryDto>> GetTopTermsByQualityAsync(GlossaryTermType type, int limit = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                // Cache first — only enabled in Production to avoid stale dev data during development
                var cacheKey = $"glossary:top:{type}:{limit}";
                var useCache = _env != null && _env.IsProduction();
                if (useCache)
                {
                    if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is List<GlossaryTermSummaryDto> cachedList)
                    {
                        _logger.LogInformation("GetTopTermsByQualityAsync: cache hit for {Key} returning {Count} items", cacheKey, cachedList.Count);
                        return cachedList;
                    }
                }

                // Base query: active terms of the requested type
                var termsQuery = _db.GlossaryTerms.AsNoTracking()
                    .Where(gt => gt.TipoTermino == type && gt.Activo)
                    .Select(gt => new
                    {
                        gt.Id,
                        gt.Nombre,
                        gt.Slug,
                        gt.FechaActualizacion,
                        MedicalLinkSintomaId = gt.MedicalLink != null ? gt.MedicalLink.SintomaId : (int?)null,
                        MedicalLinkTratamientoId = gt.MedicalLink != null ? gt.MedicalLink.TratamientoId : (int?)null,
                        // Sugerencia de NINA: se suma +1 al círculo del tipo sugerido
                        MedicalRelationSuggestedId = gt.MedicalRelationSuggestedId
                    });

                // Filter: only terms whose linked sintoma/tratamiento has RelacionEII == true
                // AND that also have at least one user relation (sintomasUsuario / tratamientoUsuario).
                // Esto asegura que, dado que no tenemos estadísticas de usuario para un TOP real,
                // solo mostramos términos que efectivamente están relacionados con usuarios.
                var filtered = termsQuery.Where(t =>
                    (
                        t.MedicalLinkSintomaId != null
                        && _db.sintomas.Any(s => s.id == t.MedicalLinkSintomaId && s.RelacionEII && !s.Eliminado)
                        && _db.sintomasUsuario.Any(su => su.idSintoma == t.MedicalLinkSintomaId && !su.Eliminado)
                    )
                    || (
                        t.MedicalLinkTratamientoId != null
                        && _db.tratamientos.Any(tr => tr.id == t.MedicalLinkTratamientoId && tr.RelacionEII && !tr.Eliminado)
                        && _db.tratamientoUsuario.Any(tu => tu.idTratamiento == t.MedicalLinkTratamientoId && !tu.Eliminado)
                    )
                );

                // Project with user relationship count + validation badges
                var projected = filtered.Select(t => new GlossaryTermSummaryDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Slug = t.Slug,
                    ShortDescription = null,
                    LastHumanUpdateDate = _db.GlossaryValidations
                        .Where(v => v.GlossaryTermId == t.Id && v.Approved)
                        .Max(v => (DateTime?)v.CreatedAt),
                    Views = 0,
                    IsValidated = true,
                    IsReviewedByHuman = true,
                    HasRelationBadge = true,
                    // Count distinct users who have this symptom/treatment in their profile (mejor indicador de relaciones reales)
                    UserRelationCount = type == GlossaryTermType.Sintoma
                        ? _db.sintomasUsuario.Where(su => su.idSintoma == t.MedicalLinkSintomaId && !su.Eliminado).Select(su => su.idUsuario).Distinct().Count()
                        : _db.tratamientoUsuario.Where(tu => tu.idTratamiento == t.MedicalLinkTratamientoId && !tu.Eliminado).Select(tu => tu.idUsuario).Distinct().Count(),
                    RelationDirectCount = _db.GlossaryValidations.Count(v => v.GlossaryTermId == t.Id && v.ValidationType == GlossaryValidationType.RelationValidation && v.MedicalRelationTypeId == MedicalRelationType.Directa && v.Approved)
                        + (t.MedicalRelationSuggestedId == MedicalRelationType.Directa ? 1 : 0),
                    RelationIndirectCount = _db.GlossaryValidations.Count(v => v.GlossaryTermId == t.Id && v.ValidationType == GlossaryValidationType.RelationValidation && v.MedicalRelationTypeId == MedicalRelationType.Indirecta && v.Approved)
                        + (t.MedicalRelationSuggestedId == MedicalRelationType.Indirecta ? 1 : 0),
                    RelationSecondaryCount = _db.GlossaryValidations.Count(v => v.GlossaryTermId == t.Id && v.ValidationType == GlossaryValidationType.RelationValidation && v.MedicalRelationTypeId == MedicalRelationType.Secundaria && v.Approved)
                        + (t.MedicalRelationSuggestedId == MedicalRelationType.Secundaria ? 1 : 0)
                });

                // Order by most user relationships first, then by name
                var ordered = projected
                    .OrderByDescending(x => x.UserRelationCount)
                    .ThenBy(x => x.Nombre)
                    .Take(limit);

                var list = await ordered.ToListAsync(cancellationToken);
                _logger.LogInformation("GetTopTermsByQualityAsync: DB returned {Count} items for type {Type}", list.Count, type);

                if (useCache)
                {
                    using (var entry = _cache.CreateEntry(cacheKey))
                    {
                        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                        entry.Value = list;
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener top terms by quality for {Type}", type);
                return new List<GlossaryTermSummaryDto>();
            }
        }

        /// <summary>
        /// Obtiene preguntas relacionadas (top N) asociadas a un síntoma o tratamiento.
        /// </summary>
        public async Task<List<RelatedQuestionDto>> GetRelatedQuestionsAsync(int? symptomId, int? treatmentId, int maxResults = 5)
        {
            try
            {
                if (!symptomId.HasValue && !treatmentId.HasValue)
                    return new List<RelatedQuestionDto>();

                IQueryable<Models.Pregunta> q = _db.Preguntas.AsNoTracking().Where(p => !p.Eliminado);

                if (symptomId.HasValue)
                {
                    q = from p in _db.Preguntas
                        join ps in _db.PreguntaSintomas on p.Id equals ps.PreguntaId
                        where ps.SintomaId == symptomId.Value && !p.Eliminado
                        select p;
                }
                else if (treatmentId.HasValue)
                {
                    q = from p in _db.Preguntas
                        join pt in _db.PreguntaTratamientos on p.Id equals pt.PreguntaId
                        where pt.TratamientoId == treatmentId.Value && !p.Eliminado
                        select p;
                }

                var result = await q
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.Slug,
                        Score = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0,
                        p.FechaCreacion
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.FechaCreacion)
                    .Take(maxResults)
                    .ToListAsync();

                return result.Select(x => new RelatedQuestionDto
                {
                    Id = x.Id,
                    Titulo = x.Titulo ?? "",
                    Slug = x.Slug ?? "",
                    Score = x.Score
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener preguntas relacionadas");
                return new List<RelatedQuestionDto>();
            }
        }
    }
}
