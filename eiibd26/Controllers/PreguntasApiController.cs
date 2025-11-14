using eiibd26.Data;
using eiibd26.DTOs;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/preguntas")]
    public class PreguntasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PreguntasApiController> _logger;
        private readonly IHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public PreguntasApiController(
              ApplicationDbContext db,
              ILogger<PreguntasApiController> logger,
              IHostEnvironment env,
              UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _logger = logger;
            _env = env;
            _userManager = userManager;
        }


        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            if (Guid.TryParse(v, out var g)) return g;
            return null;
        }


        // POST api/preguntas/{id}/eliminar
        // Marks the pregunta as Eliminado = true if the authenticated user is the owner.
        [HttpPost("{id:guid}/eliminar")]
        [Authorize]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue) return Unauthorized(new { ok = false, error = "Usuario no autenticado" });

            var p = await _db.Preguntas.FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound(new { ok = false, error = "Pregunta no encontrada" });

            if (p.UsuarioId != userId.Value)
            {
                return Forbid();
            }

            try
            {
                p.Eliminado = true;
                // Optionally set FechaModificado or UsuarioModificacion if your model has it
                _db.Preguntas.Update(p);
                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando pregunta {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al eliminar la pregunta" });
            }
        }


        // POST api/preguntas/{id}/votar
        [HttpPost("{id:guid}/votar")]
        [Authorize]
        public async Task<IActionResult> VotarPregunta(Guid id, [FromBody] VotarDto votoDto)
        {
            if (votoDto == null) return BadRequest();
            if (votoDto.Valor != 1 && votoDto.Valor != -1) return BadRequest("Valor debe ser 1 o -1");

            try
            {
                var appUser = await _userManager.GetUserAsync(User);
                if (appUser == null)
                {
                    var rawClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    _logger.LogWarning("Authenticated principal has no matching ApplicationUser. NameIdentifier={NameIdentifier}", rawClaim);
                    return Forbid("Usuario autenticado no encontrado en la base de datos.");
                }

                if (!Guid.TryParse(appUser.Id.ToString(), out var userIdGuid))
                {
                    _logger.LogError("ApplicationUser.Id is not a GUID as expected. ApplicationUser.Id={Id}", appUser.Id);
                    return Problem("Identificador de usuario inválido en el servidor.");
                }

                var pregunta = await _db.Preguntas.FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);
                if (pregunta == null) return NotFound();

                if (pregunta.UsuarioId == userIdGuid)
                    return BadRequest("No puedes votar tu propia pregunta.");

                // Look for any existing vote (including soft-deleted)
                var existing = await _db.Votos.FirstOrDefaultAsync(v =>
                    v.EntidadTipo == "pregunta" && v.EntidadId == id && v.UsuarioId == userIdGuid);

                var votoEntityType = _db.Model.FindEntityType(typeof(Voto));
                bool hasFechaCreacion = votoEntityType?.FindProperty("FechaCreacion") != null;
                bool hasFechaModificacion = votoEntityType?.FindProperty("FechaModificacion") != null;

                if (existing == null)
                {
                    // No previous row at all -> safe to insert
                    var voto = new Voto
                    {
                        Id = Guid.NewGuid(),
                        EntidadTipo = "pregunta",
                        EntidadId = id,
                        UsuarioId = userIdGuid,
                        Valor = (short)votoDto.Valor,
                        Eliminado = false
                    };
                    if (hasFechaCreacion) voto.FechaCreacion = DateTimeOffset.UtcNow;
                    _db.Votos.Add(voto);
                }
                else
                {
                    // There is a row (might be soft-deleted). New behavior:
                    // - Same value => toggle active/soft-delete (unchanged)
                    // - Opposite value => interpret as "remove my vote" (soft-delete) instead of switching sign
                    if (existing.Valor == votoDto.Valor)
                    {
                        // toggle active/soft-delete
                        existing.Eliminado = !existing.Eliminado;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                    }
                    else
                    {
                        // Opposite clicked -> remove the existing vote (soft-delete)
                        existing.Eliminado = true;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                    }
                }

                // Guardar cambios: si hay condición de carrera que provoque UNIQUE violation, la capturamos y
                // continuamos leyendo el estado actual para devolver score and userVote (evita 500 en cliente).
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    if (inner.IndexOf("UQ_Votos_Entidad_Usuario", StringComparison.OrdinalIgnoreCase) >= 0
                        || inner.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
                        || inner.IndexOf("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _logger.LogWarning(dbEx, "Unique constraint violation while saving vote for pregunta {Id}; will re-read score/userVote.", id);
                        // swallow and continue to re-read current state
                    }
                    else
                    {
                        throw;
                    }
                }

                // Compute score (sum of active votes) and the user's current vote (1, -1 or 0)
                var score = await _db.Votos
                    .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == id && !v.Eliminado)
                    .Select(v => (int?)v.Valor).SumAsync() ?? 0;

                var votoActual = await _db.Votos
                    .Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == id && v.UsuarioId == userIdGuid)
                    .OrderByDescending(v => v.FechaCreacion)
                    .FirstOrDefaultAsync();

                int userVote = 0;
                if (votoActual != null && !votoActual.Eliminado) userVote = votoActual.Valor;

                return Ok(new { score, userVote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar VotarPregunta id={Id}", id);
                var detail = _env.IsDevelopment() ? ex.ToString() : "Error interno al procesar el voto.";
                return Problem(detail: detail);
            }
        }

        // Other endpoints (GetLista, GetDetalle, Crear, CrearRespuesta, etc.) remain as previously implemented.
    }
}