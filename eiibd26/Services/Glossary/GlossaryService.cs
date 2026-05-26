using eiibd26.Models.Glossary;
using eiibd26.Services.Community;
using eiibd26.Services.Glossary.Adapters;
using eiibd26.Services.Glossary.DTOs;
using eiibd26.Services.Medico;
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
        private readonly IMedicoBadgeService _badgeService;

        public GlossaryService(
            ApplicationDbContext db,
            IMedicalDataAdapter medicalAdapter,
            ICommunityExperienceService community,
            ILogger<GlossaryService> logger,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
            IMedicoBadgeService badgeService)
        {
            _db             = db;
            _medicalAdapter = medicalAdapter;
            _community      = community;
            _logger         = logger;
            _cache          = cache;
            _env            = env;
            _badgeService   = badgeService;
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
                        TipoTermino = gt.TipoTermino,
                        // Confirmar nivel: manual > IA > valor mínimo del enum (0) para evitar null
                        NivelRelacion = gt.MedicalRelationTypeId
                            ?? gt.MedicalRelationSuggestedId
                            ?? (MedicalRelationType)0
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

                // Hook: evaluar badges automáticos si el usuario es médico con perfil vinculado
                try
                {
                    if (Guid.TryParse(userId, out var userGuid))
                    {
                        var perfilMedico = await _db.MedicosPerfilExtendido
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.UserId == userGuid && p.MedicoId != null);
                        if (perfilMedico?.MedicoId != null)
                            await _badgeService.EvaluarBadgesAutomaticosAsync(perfilMedico.MedicoId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudieron evaluar badges para usuario {UserId}", userId);
                }

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

                var meaningValidations = await _db.GlossaryValidations
                    .AsNoTracking()
                    .Where(v =>
                        v.GlossaryTermId == termId
                        && v.ValidationType == GlossaryValidationType.MeaningValidation
                        && v.Approved)
                    .Select(v => new { v.Comment, v.UserId })
                    .ToListAsync();

                var meaningCount = meaningValidations.Count;

                // Solo mostrar texto de médicos con badge verificado/reclamado — mismo filtro que ComentariosMedicos
                var meaningComments = await FilterCommentsByVerifiedDoctorAsync(
                    meaningValidations.Select(v => (v.Comment, v.UserId)));

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

                // ComentariosMedicos: validaciones aprobadas con comentario no vacío
                var rawComments = await _db.GlossaryValidations
                    .Where(v => v.GlossaryTermId == termId && v.Approved && v.Comment != null && v.Comment != "")
                    .Select(v => new { v.UserId, v.ValidationType, v.MedicalRelationTypeId, v.Comment, v.CreatedAt })
                    .ToListAsync();

                List<ValidationCommentDto> comentariosMedicos = new();
                if (rawComments.Any())
                {
                    var userGuidList = rawComments
                        .Select(v => Guid.TryParse(v.UserId, out var g) ? (Guid?)g : null)
                        .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

                    // Médico perfiles vinculados
                    var perfiles = await _db.MedicosPerfilExtendido
                        .AsNoTracking()
                        .Where(p => p.UserId.HasValue && userGuidList.Contains(p.UserId.Value) && p.MedicoId != null)
                        .Select(p => new { p.UserId, p.MedicoId })
                        .ToListAsync();

                    var medicoIds = perfiles.Where(p => p.MedicoId.HasValue).Select(p => p.MedicoId!.Value).ToList();

                    // Médicos con badge perfil_reclamado o verificado
                    var badgesVerificados = await _db.MedicosPerfilBadge
                        .AsNoTracking()
                        .Where(pb => medicoIds.Contains(pb.MedicoId))
                        .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id, (pb, b) => new { pb.MedicoId, b.Codigo })
                        .Where(x => x.Codigo == "perfil_reclamado" || x.Codigo == "verificado")
                        .Select(x => x.MedicoId)
                        .Distinct()
                        .ToListAsync();

                    // Nombres de los médicos del directorio
                    var nombresMedico = await _db.MedicosDirectorio
                        .AsNoTracking()
                        .Where(m => medicoIds.Contains(m.Id))
                        .Select(m => new { m.Id, m.NombreCompleto })
                        .ToListAsync();

                    var nombreDict = nombresMedico.ToDictionary(m => m.Id, m => m.NombreCompleto);
                    var medicoIdByUser = perfiles.Where(p => p.MedicoId.HasValue)
                        .ToDictionary(p => p.UserId!.Value, p => p.MedicoId!.Value);

                    foreach (var v in rawComments)
                    {
                        if (!Guid.TryParse(v.UserId, out var guid)) continue;
                        if (!medicoIdByUser.TryGetValue(guid, out var medicoId)) continue;

                        string display;
                        if (badgesVerificados.Contains(medicoId) && nombreDict.TryGetValue(medicoId, out var nombre))
                            display = $"Dr. {nombre}";
                        else
                            display = "Médico verificado";

                        comentariosMedicos.Add(new ValidationCommentDto
                        {
                            UserDisplay   = display,
                            ValidationType = v.ValidationType,
                            RelationType  = v.MedicalRelationTypeId,
                            Comment       = v.Comment,
                            CreatedAt     = v.CreatedAt
                        });
                    }
                }

                return new GlossaryValidationCountsDto
                {
                    CreatedByAI            = term?.CreatedByAI ?? true,
                    MeaningValidationCount = meaningCount,
                    MeaningComments        = meaningComments,
                    RelationSuggested      = term?.MedicalRelationSuggestedId,
                    AiReasoning            = aiReasoning,
                    RelationConsensus      = consensus,
                    ComentariosMedicos     = comentariosMedicos
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
                        && c.EstadoPublicacion == 1
                        && !string.IsNullOrEmpty(c.ContenidoTituloSlug)
                        && c.ContenidoTitulo.Contains(termName))
                    .OrderByDescending(c => c.ContenidoFechaInicio)
                    .Take(5)
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
                // PERF-004/005: Materializamos los términos filtrados primero (solo campos base),
                // luego resolvemos conteos de validaciones en queries batched separadas.
                // Elimina 4 subqueries correlacionadas por fila dentro del SELECT.
                var termsCandidatos = await filtered
                    .Select(t => new
                    {
                        t.Id,
                        t.Nombre,
                        t.Slug,
                        t.MedicalLinkSintomaId,
                        t.MedicalLinkTratamientoId,
                        t.MedicalRelationSuggestedId
                    })
                    .ToListAsync(cancellationToken);

                if (!termsCandidatos.Any())
                    return new List<GlossaryTermSummaryDto>();

                var termIds        = termsCandidatos.Select(t => t.Id).ToList();
                var sintomaIds     = termsCandidatos.Where(t => t.MedicalLinkSintomaId.HasValue)
                                        .Select(t => t.MedicalLinkSintomaId!.Value).ToList();
                var tratamientoIds = termsCandidatos.Where(t => t.MedicalLinkTratamientoId.HasValue)
                                        .Select(t => t.MedicalLinkTratamientoId!.Value).ToList();

                // Conteos de validaciones por término y tipo de relación — 1 sola query
                var validacionesBatch = await _db.GlossaryValidations
                    .AsNoTracking()
                    .Where(v => termIds.Contains(v.GlossaryTermId)
                                && v.ValidationType == GlossaryValidationType.RelationValidation
                                && v.Approved
                                && v.MedicalRelationTypeId != null)
                    .GroupBy(v => new { v.GlossaryTermId, v.MedicalRelationTypeId })
                    .Select(g => new { g.Key.GlossaryTermId, g.Key.MedicalRelationTypeId, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                // Última validación humana por término — 1 sola query
                var ultimaValidacionBatch = await _db.GlossaryValidations
                    .AsNoTracking()
                    .Where(v => termIds.Contains(v.GlossaryTermId) && v.Approved)
                    .GroupBy(v => v.GlossaryTermId)
                    .Select(g => new { TermId = g.Key, Last = g.Max(v => (DateTime?)v.CreatedAt) })
                    .ToDictionaryAsync(x => x.TermId, x => x.Last, cancellationToken);

                // Usuarios únicos por síntoma — 1 sola query (solo si hay términos de tipo Sintoma)
                Dictionary<int, int> usuariosPorSintoma = new();
                if (sintomaIds.Any())
                {
                    usuariosPorSintoma = await _db.sintomasUsuario
                        .AsNoTracking()
                        .Where(su => su.idSintoma.HasValue && sintomaIds.Contains(su.idSintoma.Value) && !su.Eliminado)
                        .GroupBy(su => su.idSintoma!.Value)
                        .Select(g => new { SintomaId = g.Key, Usuarios = g.Select(su => su.idUsuario).Distinct().Count() })
                        .ToDictionaryAsync(x => x.SintomaId, x => x.Usuarios, cancellationToken);
                }

                // Usuarios únicos por tratamiento — 1 sola query (solo si hay términos de tipo Tratamiento)
                Dictionary<int, int> usuariosPorTratamiento = new();
                if (tratamientoIds.Any())
                {
                    usuariosPorTratamiento = await _db.tratamientoUsuario
                        .AsNoTracking()
                        .Where(tu => tu.idTratamiento.HasValue && tratamientoIds.Contains(tu.idTratamiento.Value) && !tu.Eliminado)
                        .GroupBy(tu => tu.idTratamiento!.Value)
                        .Select(g => new { TratamientoId = g.Key, Usuarios = g.Select(tu => tu.idUsuario).Distinct().Count() })
                        .ToDictionaryAsync(x => x.TratamientoId, x => x.Usuarios, cancellationToken);
                }

                // Proyectar y calcular ranking en memoria
                var list = termsCandidatos
                    .Select(t =>
                    {
                        int GetValidationCount(MedicalRelationType rel) =>
                            validacionesBatch.Where(v => v.GlossaryTermId == t.Id && v.MedicalRelationTypeId == rel)
                                             .Sum(v => v.Count);

                        var directCount    = GetValidationCount(MedicalRelationType.Directa)
                                            + (t.MedicalRelationSuggestedId == MedicalRelationType.Directa ? 1 : 0);
                        var indirectCount  = GetValidationCount(MedicalRelationType.Indirecta)
                                            + (t.MedicalRelationSuggestedId == MedicalRelationType.Indirecta ? 1 : 0);
                        var secondaryCount = GetValidationCount(MedicalRelationType.Secundaria)
                                            + (t.MedicalRelationSuggestedId == MedicalRelationType.Secundaria ? 1 : 0);

                        int userCount = type == GlossaryTermType.Sintoma
                            ? (t.MedicalLinkSintomaId.HasValue && usuariosPorSintoma.TryGetValue(t.MedicalLinkSintomaId.Value, out var uc) ? uc : 0)
                            : (t.MedicalLinkTratamientoId.HasValue && usuariosPorTratamiento.TryGetValue(t.MedicalLinkTratamientoId.Value, out var ut) ? ut : 0);

                        return new
                        {
                            Dto = new GlossaryTermSummaryDto
                            {
                                Id                    = t.Id,
                                Nombre                = t.Nombre,
                                Slug                  = t.Slug,
                                ShortDescription      = null,
                                LastHumanUpdateDate   = ultimaValidacionBatch.TryGetValue(t.Id, out var lv) ? lv : null,
                                Views                 = 0,
                                IsValidated           = true,
                                IsReviewedByHuman     = true,
                                HasRelationBadge      = true,
                                UserRelationCount     = userCount,
                                RelationDirectCount   = directCount,
                                RelationIndirectCount = indirectCount,
                                RelationSecondaryCount = secondaryCount
                            },
                            Score = (directCount * 3) + (indirectCount * 2) + (secondaryCount * 1) + userCount
                        };
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Dto.Nombre)
                    .Take(limit)
                    .Select(x => x.Dto)
                    .ToList();

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
                        Score = _db.Votos.Where(v => v.EntidadTipo == eiibd26.Controllers.VotoTipo.Pregunta && v.EntidadId == p.Id && !v.Eliminado).Select(v => (int?)v.Valor).Sum() ?? 0,
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

        /// <summary>
        /// Filtra una colección de pares (Comment, UserId) para devolver solo los comentarios
        /// de usuarios que tienen un médico con badge perfil_reclamado o verificado.
        /// Evita mostrar texto de cuentas de prueba o usuarios no verificados.
        /// </summary>
        private async Task<List<string>> FilterCommentsByVerifiedDoctorAsync(
            IEnumerable<(string? Comment, string? UserId)> validations)
        {
            var items = validations
                .Where(v => !string.IsNullOrWhiteSpace(v.Comment))
                .ToList();

            if (!items.Any())
                return new List<string>();

            var userGuidList = items
                .Select(v => Guid.TryParse(v.UserId, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue).Select(g => g!.Value).Distinct().ToList();

            var perfiles = await _db.MedicosPerfilExtendido
                .AsNoTracking()
                .Where(p => p.UserId.HasValue && userGuidList.Contains(p.UserId.Value) && p.MedicoId != null)
                .Select(p => new { p.UserId, p.MedicoId })
                .ToListAsync();

            var medicoIds = perfiles.Where(p => p.MedicoId.HasValue).Select(p => p.MedicoId!.Value).ToList();
            if (!medicoIds.Any())
                return new List<string>();

            var badgesVerificados = await _db.MedicosPerfilBadge
                .AsNoTracking()
                .Where(pb => medicoIds.Contains(pb.MedicoId))
                .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id, (pb, b) => new { pb.MedicoId, b.Codigo })
                .Where(x => x.Codigo == "perfil_reclamado" || x.Codigo == "verificado")
                .Select(x => x.MedicoId)
                .Distinct()
                .ToListAsync();

            if (!badgesVerificados.Any())
                return new List<string>();

            var verifiedUserGuids = perfiles
                .Where(p => p.MedicoId.HasValue && badgesVerificados.Contains(p.MedicoId!.Value))
                .Select(p => p.UserId!.Value)
                .ToHashSet();

            return items
                .Where(v => Guid.TryParse(v.UserId, out var g) && verifiedUserGuids.Contains(g))
                .Select(v => v.Comment!)
                .ToList();
        }
    }
}
