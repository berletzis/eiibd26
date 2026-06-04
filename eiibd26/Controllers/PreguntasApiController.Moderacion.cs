using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    public partial class PreguntasApiController
    {
        public record MotivoDto(string? Motivo);

        // POST api/preguntas/admin/{id}/eliminar  — soft-delete sin restricción de dueño ni respuestas
        [HttpPost("admin/{id:guid}/eliminar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminEliminarPregunta(Guid id)
        {
            var p = await _db.Preguntas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound(new { ok = false, error = "Pregunta no encontrada." });

            p.Eliminado = true;
            p.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Pregunta {Id} eliminada por admin", id);
            return Ok(new { ok = true });
        }

        // POST api/preguntas/admin/{id}/deshabilitar
        [HttpPost("admin/{id:guid}/deshabilitar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminDeshabilitarPregunta(Guid id, [FromBody] MotivoDto dto)
        {
            var p = await _db.Preguntas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound(new { ok = false, error = "Pregunta no encontrada." });

            p.Deshabilitado = true;
            p.MotivoModeracion = dto?.Motivo?.Trim();
            p.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Pregunta {Id} deshabilitada: {Motivo}", id, p.MotivoModeracion);
            return Ok(new { ok = true });
        }

        // POST api/preguntas/admin/{id}/rehabilitar
        [HttpPost("admin/{id:guid}/rehabilitar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminRehabilitarPregunta(Guid id)
        {
            var p = await _db.Preguntas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return NotFound(new { ok = false, error = "Pregunta no encontrada." });

            p.Deshabilitado = false;
            p.MotivoModeracion = null;
            p.FechaModificacion = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[Moderación] Pregunta {Id} rehabilitada", id);
            return Ok(new { ok = true });
        }
    }
}
