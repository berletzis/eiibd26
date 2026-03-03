using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.DTOs;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/respuestas")]
    public class RespuestasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RespuestasApiController> _logger;
        private readonly IHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public RespuestasApiController(
              ApplicationDbContext db,
              ILogger<RespuestasApiController> logger,
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

        // POST api/respuestas/{id}/eliminar
        // Marks the respuesta as Eliminado = true if the authenticated user is the owner.
        [HttpPost("{id:guid}/eliminar")]
        [Authorize]
        public async Task<IActionResult> Eliminar(Guid id)
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue) return Unauthorized(new { ok = false, error = "Usuario no autenticado" });

            var r = await _db.Respuestas.FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound(new { ok = false, error = "Respuesta no encontrada" });

            if (r.UsuarioId != userId.Value)
            {
                return Forbid();
            }

            try
            {
                r.Eliminado = true;
                _db.Respuestas.Update(r);
                await _db.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando respuesta {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al eliminar la respuesta" });
            }
        }

        // POST /api/respuestas
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearRespuesta([FromBody] CrearRespuestaDto dto)
        {
            if (dto == null) return BadRequest();
            if (!ModelState.IsValid) return BadRequest(ModelState);

            Guid preguntaId;
            var header = HttpContext.Request.Headers["X-Pregunta-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header) && Guid.TryParse(header, out preguntaId))
            {
            }
            else if (!string.IsNullOrEmpty(HttpContext.Request.Query["preguntaId"]) && Guid.TryParse(HttpContext.Request.Query["preguntaId"], out preguntaId))
            {
            }
            else if (dto.PreguntaId.HasValue)
            {
                preguntaId = dto.PreguntaId.Value;
            }
            else
            {
                return BadRequest("preguntaId no especificado. Envíalo como header 'X-Pregunta-Id', query 'preguntaId' o en el body JSON como 'preguntaId'.");
            }

            var pregunta = await _db.Preguntas.FirstOrDefaultAsync(p => p.Id == preguntaId && !p.Eliminado);
            if (pregunta == null) return BadRequest("Pregunta no encontrada");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Forbid();

            try
            {
                var respuesta = new Respuesta
                {
                    Id = Guid.NewGuid(),
                    PreguntaId = preguntaId,
                    UsuarioId = userId,
                    Cuerpo = dto.Cuerpo?.Trim(),
                    FechaCreacion = DateTimeOffset.UtcNow,
                    EsAceptada = false,
                    Eliminado = false
                };

                var effectiveParent = dto.ParentId;
                if (effectiveParent.HasValue)
                {
                    var entityType = _db.Model.FindEntityType(typeof(Respuesta));
                    if (entityType != null)
                    {
                        string[] parentCandidates = new[] { "ParentRespuestaId", "ParentId", "RespuestaPadreId" };
                        foreach (var cand in parentCandidates)
                        {
                            if (entityType.FindProperty(cand) != null)
                            {
                                var propInfo = typeof(Respuesta).GetProperty(cand);
                                if (propInfo != null && propInfo.PropertyType == typeof(Guid?))
                                {
                                    propInfo.SetValue(respuesta, effectiveParent);
                                }
                                else
                                {
                                    _db.Entry(respuesta).Property(cand).CurrentValue = effectiveParent;
                                }
                                break;
                            }
                        }
                    }
                }

                _db.Respuestas.Add(respuesta);
                await _db.SaveChangesAsync();

                // Devuelvo OK con el id creado (evita 500 por Location header generation)
                return Ok(new { id = respuesta.Id });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DbUpdateException al crear respuesta para pregunta {PreguntaId}", preguntaId);
                var detail = _env.IsDevelopment() ? dbEx.InnerException?.Message ?? dbEx.Message : "Error interno al crear la respuesta.";
                return Problem(detail: detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear respuesta para pregunta {PreguntaId}", preguntaId);
                var detail = _env.IsDevelopment() ? ex.ToString() : "Error interno al crear la respuesta.";
                return Problem(detail: detail);
            }
        }

        // POST /api/respuestas/{id}/votar
        [HttpPost("{id:guid}/votar")]
        [Authorize]
        public async Task<IActionResult> VotarRespuesta(Guid id, [FromBody] VotarDto dto)
        {
            if (dto == null) return BadRequest();
            if (dto.Valor != 1 && dto.Valor != -1) return BadRequest("Valor debe ser 1 o -1");

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

                var respuesta = await _db.Respuestas.FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);
                if (respuesta == null) return NotFound();

                if (respuesta.UsuarioId == userIdGuid)
                {
                    return BadRequest("No puedes votar tu propia respuesta.");
                }

                // Look for any existing vote (including soft-deleted)
                var existing = await _db.Votos.FirstOrDefaultAsync(v =>
                    v.EntidadTipo == "respuesta" && v.EntidadId == id && v.UsuarioId == userIdGuid);

                var votoEntityType = _db.Model.FindEntityType(typeof(Voto));
                bool hasFechaCreacion = votoEntityType?.FindProperty("FechaCreacion") != null;
                bool hasFechaModificacion = votoEntityType?.FindProperty("FechaModificacion") != null;

                if (existing == null)
                {
                    var voto = new Voto
                    {
                        Id = Guid.NewGuid(),
                        EntidadTipo = "respuesta",
                        EntidadId = id,
                        UsuarioId = userIdGuid,
                        Valor = (short)dto.Valor,
                        Eliminado = false
                    };
                    if (hasFechaCreacion) voto.FechaCreacion = DateTimeOffset.UtcNow;
                    _db.Votos.Add(voto);
                }
                else
                {
                    // Usuario ya votó previamente
                    if (existing.Valor == dto.Valor)
                    {
                        // Intenta votar lo mismo (ej: +1 cuando ya votó +1)
                        // Toggle: activo ↔ cancelado (permite re-votación flexible)
                        existing.Eliminado = !existing.Eliminado;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                    }
                    else
                    {
                        // Intenta cambiar el voto (ej: -1 cuando ya votó +1)
                        // Eliminar el voto anterior para permitir crear uno nuevo
                        existing.Eliminado = true;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                    }
                }

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
                        _logger.LogWarning(dbEx, "Unique constraint violation while saving vote for respuesta {Id}; will re-read score/userVote.", id);
                        // swallow and continue
                    }
                    else
                    {
                        throw;
                    }
                }

                var score = await _db.Votos
                    .Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == id && !v.Eliminado)
                    .Select(v => (int?)v.Valor)
                    .SumAsync() ?? 0;

                var votoActual = await _db.Votos
                    .Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == id && v.UsuarioId == userIdGuid)
                    .OrderByDescending(v => v.FechaCreacion)
                    .FirstOrDefaultAsync();

                int userVote = 0;
                if (votoActual != null && !votoActual.Eliminado) userVote = votoActual.Valor;

                return Ok(new { score, userVote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar VotarRespuesta id={Id}", id);
                var detail = _env.IsDevelopment() ? ex.ToString() : "Error interno al procesar el voto.";
                return Problem(detail: detail);
            }
        }
    }
}