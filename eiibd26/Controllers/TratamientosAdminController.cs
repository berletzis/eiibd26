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

                // ⭐ ACTUALIZAR EL NOMBRE SI SE TRADUJO
                if (!string.IsNullOrWhiteSpace(nombreTraducido) && 
                    !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Traduciendo nombre de '{NombreOriginal}' a '{NombreTraducido}'", 
                        tratamiento.nombre, nombreTraducido);
                    tratamiento.nombre = nombreTraducido;
                }

                // Actualizar el tratamiento
                tratamiento.DescripcionIA = descripcion;
                tratamiento.ValidadoIA = true;
                tratamiento.RelacionEII = relacionEII;
                tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                tratamiento.Fuentes = _aiService.UltimasFuentes;
                tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                tratamiento.fechaModificado = DateTime.Now;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Descripción IA guardada exitosamente para tratamiento {Id}", id);

                return Ok(new 
                { 
                    ok = true, 
                    descripcion,
                    relacionEII,
                    relacionEIITexto = tratamiento.RelacionEIIDescripcion,
                    fuentes = tratamiento.Fuentes,
                    nombreTraducido = nombreTraducido // ⭐ RETORNAR NOMBRE TRADUCIDO
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
                fuentes = tratamiento.Fuentes ?? "" // ⭐ AGREGAR FUENTES
            });
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

                        // ⭐ ACTUALIZAR EL NOMBRE SI SE TRADUJO
                        var nombreOriginal = tratamiento.nombre;
                        if (!string.IsNullOrWhiteSpace(nombreTraducido) && 
                            !nombreTraducido.Equals(tratamiento.nombre, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Traduciendo nombre de '{NombreOriginal}' a '{NombreTraducido}'", 
                                tratamiento.nombre, nombreTraducido);
                            tratamiento.nombre = nombreTraducido;
                        }

                        // Actualizar el tratamiento
                        tratamiento.DescripcionIA = descripcion;
                        tratamiento.ValidadoIA = true;
                        tratamiento.ValidadoHumano = false; // ⭐ Resetear validación humana
                        tratamiento.RelacionEII = relacionEII;
                        tratamiento.RelacionEIIDescripcion = _aiService.UltimaExplicacionEII;
                        tratamiento.Fuentes = _aiService.UltimasFuentes;
                        tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                        tratamiento.fechaModificado = DateTime.Now;

                        await _db.SaveChangesAsync(cancellationToken);

                        resultados.Add(new BatchResultItem
                        {
                            Id = tratamiento.id,
                            Nombre = tratamiento.nombre, // ⭐ Usar el nombre actualizado (traducido)
                            NombreOriginal = nombreOriginal != tratamiento.nombre ? nombreOriginal : null,
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
            public string? NombreOriginal { get; set; } // ⭐ NUEVO: Para mostrar traducciones
            public bool Success { get; set; }
            public string? Error { get; set; }
            public bool RelacionEII { get; set; }
        }
    }
}
