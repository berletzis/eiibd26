using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/sintomas")]
    public class SintomasAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ISintomasTratamientosAiService _aiService;
        private readonly ILogger<SintomasAdminController> _logger;

        public SintomasAdminController(
            ApplicationDbContext db,
            ISintomasTratamientosAiService aiService,
            ILogger<SintomasAdminController> logger)
        {
            _db = db;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Genera descripción IA para un síntoma
        /// POST /api/admin/sintomas/{id}/generate-ia-description
        /// </summary>
        [HttpPost("{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateIaDescription(int id, CancellationToken cancellationToken)
        {
            try
            {
                var sintoma = await _db.sintomas
                    .FirstOrDefaultAsync(s => s.id == id && !s.Eliminado, cancellationToken);

                if (sintoma == null)
                    return NotFound(new { ok = false, error = "Síntoma no encontrado" });

                if (string.IsNullOrWhiteSpace(sintoma.nombre))
                    return BadRequest(new { ok = false, error = "El síntoma no tiene nombre" });

                _logger.LogInformation("Generando descripción IA para síntoma {Id}: {Nombre}", id, sintoma.nombre);

                var (descripcion, relacionEII) = await _aiService.GenerarDescripcionSintomaAsync(
                    sintoma.nombre, 
                    cancellationToken);

                // Actualizar el síntoma
                sintoma.DescripcionIA = descripcion;
                sintoma.ValidadoIA = true;
                sintoma.RelacionEII = relacionEII;
                sintoma.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                sintoma.Fuentes = _aiService.UltimasFuentes; // ⭐ NUEVO: GUARDAR FUENTES
                sintoma.FechaActualizacionIA = DateTime.UtcNow;
                sintoma.fechaModificado = DateTime.Now;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Descripción IA guardada exitosamente para síntoma {Id}", id);

                return Ok(new 
                { 
                    ok = true, 
                    descripcion,
                    relacionEII,
                    relacionEIITexto = sintoma.RelacionEIIDescripcion,
                    fuentes = sintoma.Fuentes // ⭐ AGREGAR FUENTES
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para síntoma {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar la descripción: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un síntoma por ID
        /// GET /api/admin/sintomas/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSintoma(int id, CancellationToken cancellationToken)
        {
            var sintoma = await _db.sintomas
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.id == id, cancellationToken);

            if (sintoma == null)
                return NotFound(new { ok = false, error = "Síntoma no encontrado" });

            return Ok(new 
            {
                ok = true,
                id = sintoma.id,
                nombre = sintoma.nombre ?? "",
                idPadre = sintoma.idPadre,
                idIdioma = sintoma.idIdioma,
                icono = sintoma.icono ?? "",
                eliminado = sintoma.Eliminado,
                descripcionIA = sintoma.DescripcionIA ?? "",
                validadoIA = sintoma.ValidadoIA,
                validadoHumano = sintoma.ValidadoHumano,
                relacionEII = sintoma.RelacionEII,
                relacionEIIDescripcion = sintoma.RelacionEIIDescripcion ?? "",
                fuentes = sintoma.Fuentes ?? "" // ⭐ AGREGAR FUENTES
            });
        }

        /// <summary>
        /// Actualiza un síntoma
        /// PUT /api/admin/sintomas/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSintoma(int id, [FromBody] UpdateSintomaRequest request, CancellationToken cancellationToken)
        {
            var sintoma = await _db.sintomas
                .FirstOrDefaultAsync(s => s.id == id, cancellationToken);

            if (sintoma == null)
                return NotFound(new { ok = false, error = "Síntoma no encontrado" });

            sintoma.nombre = request.Nombre;
            sintoma.idPadre = request.IdPadre;
            sintoma.idIdioma = request.IdIdioma;
            sintoma.icono = request.Icono;
            sintoma.Eliminado = request.Eliminado;
            sintoma.DescripcionIA = request.DescripcionIA;
            sintoma.ValidadoHumano = request.ValidadoHumano;
            sintoma.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { ok = true });
        }

        /// <summary>
        /// Genera descripciones IA para múltiples síntomas
        /// POST /api/admin/sintomas/batch-generate-ia
        /// </summary>
        [HttpPost("batch-generate-ia")]
        public async Task<IActionResult> BatchGenerateIaDescriptions([FromBody] BatchGenerateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var resultados = new List<BatchResultItem>();

                // Obtener los siguientes N síntomas sin descripción IA (no eliminados)
                var sintomas = await _db.sintomas
                    .Where(s => !s.Eliminado)
                    .Where(s => string.IsNullOrEmpty(s.DescripcionIA) || !s.ValidadoIA)
                    .OrderBy(s => s.id)
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Procesando batch de {Count} síntomas. Skip: {Skip}", sintomas.Count, request.Skip);

                foreach (var sintoma in sintomas)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(sintoma.nombre))
                        {
                            resultados.Add(new BatchResultItem
                            {
                                Id = sintoma.id,
                                Nombre = sintoma.nombre ?? "Sin nombre",
                                Success = false,
                                Error = "El síntoma no tiene nombre"
                            });
                            continue;
                        }

                        _logger.LogInformation("Generando descripción IA para síntoma {Id}: {Nombre}", sintoma.id, sintoma.nombre);

                        var (descripcion, relacionEII) = await _aiService.GenerarDescripcionSintomaAsync(
                            sintoma.nombre, 
                            cancellationToken);

                        // Actualizar el síntoma
                        sintoma.DescripcionIA = descripcion;
                        sintoma.ValidadoIA = true;
                        sintoma.ValidadoHumano = false; // ⭐ Resetear validación humana
                        sintoma.RelacionEII = relacionEII;
                        sintoma.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                        sintoma.FechaActualizacionIA = DateTime.UtcNow;
                        sintoma.fechaModificado = DateTime.Now;

                        await _db.SaveChangesAsync(cancellationToken);

                        resultados.Add(new BatchResultItem
                        {
                            Id = sintoma.id,
                            Nombre = sintoma.nombre,
                            Success = true,
                            RelacionEII = relacionEII
                        });

                        _logger.LogInformation("Síntoma {Id} actualizado exitosamente", sintoma.id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al procesar síntoma {Id}: {Nombre}", sintoma.id, sintoma.nombre);

                        resultados.Add(new BatchResultItem
                        {
                            Id = sintoma.id,
                            Nombre = sintoma.nombre ?? "Sin nombre",
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                // Contar cuántos faltan por procesar
                var totalPendientes = await _db.sintomas
                    .Where(s => !s.Eliminado)
                    .Where(s => string.IsNullOrEmpty(s.DescripcionIA) || !s.ValidadoIA)
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
                _logger.LogError(ex, "Error en procesamiento batch de síntomas");
                return StatusCode(500, new { ok = false, error = "Error en procesamiento: " + ex.Message });
            }
        }

        public class UpdateSintomaRequest
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
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool RelacionEII { get; set; }
        }
    }
}
