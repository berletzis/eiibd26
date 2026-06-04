using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    public partial class RespuestasApiController
    {
        public record RespuestaMotivoDto(string? Motivo);

        // POST api/respuestas/admin/{id}/eliminar
        [HttpPost("admin/{id:guid}/eliminar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminEliminarRespuesta(Guid id)
        {
            var r = await _db.Respuestas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return NotFound(new { ok = false, error = "Respuesta no encontrada." });

            r.Eliminado = true;
            r.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Respuesta {Id} eliminada por admin", id);
            return Ok(new { ok = true });
        }

        // POST api/respuestas/admin/{id}/deshabilitar
        [HttpPost("admin/{id:guid}/deshabilitar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminDeshabilitarRespuesta(Guid id, [FromBody] RespuestaMotivoDto dto)
        {
            var r = await _db.Respuestas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return NotFound(new { ok = false, error = "Respuesta no encontrada." });

            r.Deshabilitado = true;
            r.MotivoModeracion = dto?.Motivo?.Trim();
            r.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Respuesta {Id} deshabilitada: {Motivo}", id, r.MotivoModeracion);
            return Ok(new { ok = true });
        }

        // POST api/respuestas/admin/{id}/rehabilitar
        [HttpPost("admin/{id:guid}/rehabilitar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminRehabilitarRespuesta(Guid id)
        {
            var r = await _db.Respuestas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (r is null) return NotFound(new { ok = false, error = "Respuesta no encontrada." });

            r.Deshabilitado = false;
            r.MotivoModeracion = null;
            r.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Respuesta {Id} rehabilitada", id);
            return Ok(new { ok = true });
        }
    }
}
