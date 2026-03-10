using eiibd26.Models.Glossary;
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
        private readonly ILogger<GlossaryService> _logger;

        public GlossaryService(
            ApplicationDbContext db,
            IMedicalDataAdapter medicalAdapter,
            ILogger<GlossaryService> logger)
        {
            _db = db;
            _medicalAdapter = medicalAdapter;
            _logger = logger;
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
                // 1. Buscar término en glosario
                var term = await _db.GlossaryTerms
                    .AsNoTracking()
                    .Include(gt => gt.MedicalLink)
                    .FirstOrDefaultAsync(gt => gt.Slug == slug && gt.Activo);

                if (term == null)
                {
                    _logger.LogWarning("Término con slug '{Slug}' no encontrado", slug);
                    return null;
                }

                var detail = new GlossaryTermDetailDto
                {
                    Id = term.Id,
                    Nombre = term.Nombre,
                    Slug = term.Slug,
                    TipoTermino = term.TipoTermino
                };

                // 2. Leer definición médica a través del adapter (desacoplado)
                if (term.MedicalLink != null)
                {
                    if (term.MedicalLink.SintomaId.HasValue)
                    {
                        detail.DefinicionMedica = await _medicalAdapter.GetSymptomDefinitionAsync(
                            term.MedicalLink.SintomaId.Value);
                    }
                    else if (term.MedicalLink.TratamientoId.HasValue)
                    {
                        detail.DefinicionMedica = await _medicalAdapter.GetTreatmentDefinitionAsync(
                            term.MedicalLink.TratamientoId.Value);
                    }
                }

                // 3. Buscar artículos relacionados del CMS
                detail.ArticulosRelacionados = await GetRelatedContentsAsync(term.Nombre, 10);

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
