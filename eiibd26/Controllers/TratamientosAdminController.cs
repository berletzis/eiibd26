using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Models.Glossary;
using eiibd26.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/tratamientos")]
    public class TratamientosAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ISintomasTratamientosAiService _aiService;
        private readonly ILogger<TratamientosAdminController> _logger;

        public TratamientosAdminController(
            ApplicationDbContext db,
            ISintomasTratamientosAiService aiService,
            ILogger<TratamientosAdminController> logger)
        {
            _db = db;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Genera descripción IA para un tratamiento
        /// POST /api/admin/tratamientos/{id}/generate-ia-description
        /// </summary>
        [HttpPost("{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateIaDescription(int id, CancellationToken cancellationToken)
        {
            try
            {
                var tratamiento = await _db.tratamientos
                    .FirstOrDefaultAsync(t => t.id == id && !t.Eliminado, cancellationToken);

                if (tratamiento == null)
                    return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

                if (string.IsNullOrWhiteSpace(tratamiento.nombre))
                    return BadRequest(new { ok = false, error = "El tratamiento no tiene nombre" });

                _logger.LogInformation("Generando descripción IA para tratamiento {Id}: {Nombre}", id, tratamiento.nombre);

                var (descripcion, relacionEII, nombreTraducido) = await _aiService.GenerarDescripcionTratamientoAsync(
                    tratamiento.nombre, 
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
                    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "IA sugirió nombre '{NombreTraducido}' para tratamiento '{NombreOriginal}' — pendiente aprobación admin.",
                        nombreTraducido, tratamiento.nombre);
                    tratamiento.NombreSugeridoIA = nombreTraducido;
                    tratamiento.ValidadoHumano = false;
                }

                tratamiento.DescripcionIA = descripcion;
                tratamiento.ValidadoIA = true;
                tratamiento.RelacionEII = relacionEII;
                tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                tratamiento.Fuentes = _aiService.UltimasFuentes;
                tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                tratamiento.fechaModificado = DateTime.Now;

                await _db.SaveChangesAsync(cancellationToken);

                // Propagar nivel de relación y razonamiento al GlossaryTerm vinculado
                await PropagateToGlossaryTermAsync(tratamientoId: id, cancellationToken);

                _logger.LogInformation("Descripción IA guardada exitosamente para tratamiento {Id}", id);

                return Ok(new
                {
                    ok = true,
                    descripcion,
                    relacionEII,
                    relacionEIITexto  = tratamiento.RelacionEIIDescripcion,
                    nivelRelacion     = _aiService.UltimoNivelRelacion?.ToString(),
                    razonamiento      = _aiService.UltimoRazonamiento,
                    fuentes           = tratamiento.Fuentes,
                    nombreTraducido   = nombreTraducido
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para tratamiento {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar la descripción: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un tratamiento por ID
        /// GET /api/admin/tratamientos/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTratamiento(int id, CancellationToken cancellationToken)
        {
            var tratamiento = await _db.tratamientos
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.id == id, cancellationToken);

            if (tratamiento == null)
                return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

            return Ok(new 
            {
                ok = true,
                id = tratamiento.id,
                nombre = tratamiento.nombre ?? "",
                idPadre = tratamiento.idPadre,
                idIdioma = tratamiento.idIdioma,
                icono = tratamiento.icono ?? "",
                eliminado = tratamiento.Eliminado,
                descripcionIA = tratamiento.DescripcionIA ?? "",
                validadoIA = tratamiento.ValidadoIA,
                validadoHumano = tratamiento.ValidadoHumano,
                relacionEII = tratamiento.RelacionEII,
                relacionEIIDescripcion = tratamiento.RelacionEIIDescripcion ?? "",
                fuentes = tratamiento.Fuentes ?? ""
            });
        }

        /// <summary>
        /// Datos del GlossaryTerm vinculado al tratamiento: niveles de relación y validaciones.
        /// GET /api/admin/tratamientos/{id}/glossary
        /// </summary>
        [HttpGet("{id}/glossary")]
        public async Task<IActionResult> GetGlossaryData(int id, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .AsNoTracking()
                    .Where(l => l.TratamientoId == id)
                    .Select(l => new { l.GlossaryTermId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (link == null)
                    return Ok(new { ok = true, hasGlossaryTerm = false });

                int? nivelSugerido  = null;
                int? nivelConfirmado = null;
                string? aiReasoning = null;
                bool createdByAI    = true;
                try
                {
                    var term = await _db.GlossaryTerms
                        .AsNoTracking()
                        .Where(t => t.Id == link.GlossaryTermId)
                        .Select(t => new
                        {
                            t.CreatedByAI,
                            SuggestedId  = t.MedicalRelationSuggestedId.HasValue ? (int?)t.MedicalRelationSuggestedId : null,
                            ConfirmedId  = t.MedicalRelationTypeId.HasValue      ? (int?)t.MedicalRelationTypeId      : null,
                            t.AiReasoning
                        })
                        .FirstOrDefaultAsync(cancellationToken);
                    if (term != null)
                    {
                        createdByAI     = term.CreatedByAI;
                        nivelSugerido   = term.SuggestedId;
                        nivelConfirmado = term.ConfirmedId;
                        aiReasoning     = term.AiReasoning;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Columnas nuevas no disponibles aún (GlossaryTerm {Id})", link.GlossaryTermId);
                }

                var validations = new List<object>();
                try
                {
                    var raw = await _db.GlossaryValidations
                        .AsNoTracking()
                        .Where(v => v.GlossaryTermId == link.GlossaryTermId && v.Approved)
                        .OrderByDescending(v => v.CreatedAt)
                        .Select(v => new
                        {
                            v.UserId,
                            ValidationType = (int)v.ValidationType,
                            RelationTypeId = v.MedicalRelationTypeId.HasValue ? (int?)v.MedicalRelationTypeId : null,
                            v.Comment,
                            v.CreatedAt
                        })
                        .ToListAsync(cancellationToken);

                    var userGuids = raw
                        .Select(v => v.UserId)
                        .Distinct()
                        .Where(uid => Guid.TryParse(uid, out _))
                        .Select(Guid.Parse)
                        .ToList();

                    var nameDict = await _db.Users
                        .AsNoTracking()
                        .Where(u => userGuids.Contains(u.Id))
                        .Select(u => new { Id = u.Id.ToString(), Display = u.UserName ?? u.Email ?? "Usuario" })
                        .ToDictionaryAsync(u => u.Id.ToLowerInvariant(), u => u.Display, cancellationToken);

                    validations = raw.Select(v => (object)new
                    {
                        userDisplay    = nameDict.TryGetValue(v.UserId.ToLowerInvariant(), out var n) ? n : "Usuario",
                        validationType = v.ValidationType,
                        relationTypeId = v.RelationTypeId,
                        comment        = v.Comment,
                        createdAt      = v.CreatedAt
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "GlossaryValidation no disponible aún (término {Id})", link.GlossaryTermId);
                }

                return Ok(new
                {
                    ok              = true,
                    hasGlossaryTerm = true,
                    glossaryTermId  = link.GlossaryTermId,
                    createdByAI,
                    nivelSugerido,
                    nivelConfirmado,
                    aiReasoning,
                    validations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos de glosario para tratamiento {Id}", id);
                return Ok(new { ok = true, hasGlossaryTerm = false });
            }
        }

        /// <summary>
        /// Actualiza un tratamiento
        /// PUT /api/admin/tratamientos/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTratamiento(int id, [FromBody] UpdateTratamientoRequest request, CancellationToken cancellationToken)
        {
            var tratamiento = await _db.tratamientos
                .FirstOrDefaultAsync(t => t.id == id, cancellationToken);

            if (tratamiento == null)
                return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

            tratamiento.nombre = request.Nombre;
            tratamiento.idPadre = request.IdPadre;
            tratamiento.idIdioma = request.IdIdioma;
            tratamiento.icono = request.Icono;
            tratamiento.Eliminado = request.Eliminado;
            tratamiento.DescripcionIA = request.DescripcionIA;
            tratamiento.ValidadoHumano = request.ValidadoHumano;
            tratamiento.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { ok = true });
        }

        /// <summary>
        /// Genera descripciones IA para múltiples tratamientos
        /// POST /api/admin/tratamientos/batch-generate-ia
        /// </summary>
        [HttpPost("batch-generate-ia")]
        public async Task<IActionResult> BatchGenerateIaDescriptions([FromBody] BatchGenerateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var resultados = new List<BatchResultItem>();

                // Obtener los siguientes N tratamientos sin descripción IA (no eliminados)
                var tratamientos = await _db.tratamientos
                    .Where(t => !t.Eliminado)
                    .Where(t => string.IsNullOrEmpty(t.DescripcionIA) || !t.ValidadoIA)
                    .OrderBy(t => t.id)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Procesando batch de {Count} tratamientos. Skip: {Skip}", tratamientos.Count, request.Skip);

                foreach (var tratamiento in tratamientos)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(tratamiento.nombre))
                        {
                            resultados.Add(new BatchResultItem
                            {
                                Id = tratamiento.id,
                                Nombre = tratamiento.nombre ?? "Sin nombre",
                                Success = false,
                                Error = "El tratamiento no tiene nombre"
                            });
                            continue;
                        }

                        _logger.LogInformation("Generando descripción IA para tratamiento {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);

                        var (descripcion, relacionEII, nombreTraducido) = await _aiService.GenerarDescripcionTratamientoAsync(
                            tratamiento.nombre, 
                            cancellationToken);

                        var nombreOriginal = tratamiento.nombre;
                        if (!string.IsNullOrWhiteSpace(nombreTraducido) &&
                            !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation(
                                "IA sugirió nombre '{NombreTraducido}' para tratamiento '{NombreOriginal}' — pendiente aprobación admin.",
                                nombreTraducido, tratamiento.nombre);
                            tratamiento.NombreSugeridoIA = nombreTraducido;
                            tratamiento.ValidadoHumano = false;
                        }

                        // Actualizar el tratamiento
                        tratamiento.DescripcionIA = descripcion;
                        tratamiento.ValidadoIA = true;
                        tratamiento.ValidadoHumano = false; // Resetear validación humana
                        tratamiento.RelacionEII = relacionEII;
                        tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                        tratamiento.Fuentes = _aiService.UltimasFuentes;
                        tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                        tratamiento.fechaModificado = DateTime.Now;

                        await _db.SaveChangesAsync(cancellationToken);

                        await PropagateToGlossaryTermAsync(tratamientoId: tratamiento.id, cancellationToken);

                        resultados.Add(new BatchResultItem
                        {
                            Id = tratamiento.id,
                            Nombre = tratamiento.nombre,
                            NombreSugeridoIA = tratamiento.NombreSugeridoIA,
                            Success = true,
                            RelacionEII = relacionEII
                        });

                        _logger.LogInformation("Tratamiento {Id} actualizado exitosamente", tratamiento.id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar tratamiento {Id}: {Nombre}", tratamiento.id, tratamiento.nombre);

                        resultados.Add(new BatchResultItem
                        {
                            Id = tratamiento.id,
                            Nombre = tratamiento.nombre ?? "Sin nombre",
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                // Contar cuántos faltan por procesar
                var totalPendientes = await _db.tratamientos
                    .Where(t => !t.Eliminado)
                    .Where(t => string.IsNullOrEmpty(t.DescripcionIA) || !t.ValidadoIA)
                    .CountAsync(cancellationToken);

                return Ok(new 
                { 
                    ok = true,
                    procesados = resultados.Count,
                    exitosos = resultados.Count(r => r.Success),
                    fallidos = resultados.Count(r => !r.Success),
                    pendientes = totalPendientes,
                    resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento batch de tratamientos");
                return StatusCode(500, new { ok = false, error = "Error en procesamiento: " + ex.Message });
            }
        }

        /// <summary>
        /// Busca el GlossaryTerm vinculado al tratamiento y actualiza nivel + razonamiento de NINA.
        /// </summary>
        private async Task PropagateToGlossaryTermAsync(int tratamientoId, CancellationToken cancellationToken)
        {
            try
            {
                var link = await _db.GlossaryTermMedicalLinks
                    .Include(l => l.GlossaryTerm)
                    .FirstOrDefaultAsync(l => l.TratamientoId == tratamientoId, cancellationToken);

                if (link?.GlossaryTerm == null)
                    return;

                link.GlossaryTerm.MedicalRelationSuggestedId = _aiService.UltimoNivelRelacion;
                link.GlossaryTerm.AiReasoning                = _aiService.UltimoRazonamiento;
                link.GlossaryTerm.CreatedByAI                = true;
                link.GlossaryTerm.FechaActualizacion         = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "GlossaryTerm {TermId} actualizado: nivel={Nivel}, razonamiento={Razonamiento}",
                    link.GlossaryTerm.Id,
                    _aiService.UltimoNivelRelacion,
                    _aiService.UltimoRazonamiento);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo propagar nivel/razonamiento al GlossaryTerm para tratamiento {Id}", tratamientoId);
            }
        }

        public class UpdateTratamientoRequest
        {
            public string Nombre { get; set; } = "";
            public int? IdPadre { get; set; }
            public int? IdIdioma { get; set; }
            public string? Icono { get; set; }
            public bool Eliminado { get; set; }
            public string? DescripcionIA { get; set; }
            public bool ValidadoHumano { get; set; }
        }

        public class BatchGenerateRequest
        {
            public int Skip { get; set; } = 0;
            public int Take { get; set; } = 10;
        }

        public class BatchResultItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";
            public string? NombreSugeridoIA { get; set; }
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool RelacionEII { get; set; }
        }
    }
}
