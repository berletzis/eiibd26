using eiibd26.Data;
using eiibd26.DTOs;
using eiibd26.Models;
using eiibd26.Helpers;
using Hangfire;
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
    public partial class PreguntasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PreguntasApiController> _logger;
        private readonly IHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBackgroundJobClient _backgroundJobs;

        public PreguntasApiController(
              ApplicationDbContext db,
              ILogger<PreguntasApiController> logger,
              IHostEnvironment env,
              UserManager<ApplicationUser> userManager,
              IBackgroundJobClient backgroundJobs)
        {
            _db = db;
            _logger = logger;
            _env = env;
            _userManager = userManager;
            _backgroundJobs = backgroundJobs;
        }

        private Guid? GetUserIdGuid()
        {
            var v = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(v)) return null;
            if (Guid.TryParse(v, out var g)) return g;
            return null;
        }

        // ===== NUEVO: POST api/preguntas (crear pregunta) =====
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CrearPregunta([FromBody] CrearPreguntaDto dto)
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue)
                return Unauthorized(new { ok = false, error = "Usuario no autenticado" });

            if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Cuerpo))
                return BadRequest(new { ok = false, error = "Título y cuerpo son requeridos" });

            if (dto.Titulo.Length > 300)
                return BadRequest(new { ok = false, error = "Título muy largo (máx. 300 caracteres)" });

            try
            {
                // ===== GENERAR SLUG ÚNICO =====
                var slug = await SlugHelper.GenerateUniqueSlugForPregunta(_db, dto.Titulo);
                // ==============================

                var pregunta = new Pregunta
                {
                    Id = Guid.NewGuid(),
                    Titulo = dto.Titulo.Trim(),
                    Cuerpo = dto.Cuerpo.Trim(),
                    Slug = slug,  // ← ASIGNAR SLUG ÚNICO
                    UsuarioId = userId.Value,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    Eliminado = false,
                    Resuelta = false
                };

                _db.Preguntas.Add(pregunta);
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Pregunta creada {PreguntaId} con slug '{Slug}'", 
                    pregunta.Id, pregunta.Slug);

                // FUNC-017: Encolar job de IA con Hangfire para persistencia, retry y estado.
                // Reemplaza el fire-and-forget con Task.Factory.StartNew que perdía errores y no tenía retry.
                // FUNC-018: Idempotency check — no encolar si ya existe respuesta IA para esta pregunta.
                var yaExisteRespuestaIA = await _db.Respuestas
                    .AnyAsync(r => r.PreguntaId == pregunta.Id && r.EsIA && !r.Eliminado);

                if (!yaExisteRespuestaIA)
                {
                    var preguntaIdCapture = pregunta.Id;
                    _backgroundJobs.Enqueue<eiibd26.Jobs.AiAnswerJob>(
                        job => job.ProcesarPreguntaAsync(preguntaIdCapture));
                    _logger.LogInformation("AI job enqueued via Hangfire for pregunta {PreguntaId}", pregunta.Id);
                }
                // =============================================

                return Ok(new { ok = true, id = pregunta.Id, slug = pregunta.Slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear pregunta");
                return StatusCode(500, new { ok = false, error = "Error al crear la pregunta" });
            }
        }

        // ===== NUEVO: PUT api/preguntas/{id} (actualizar pregunta) =====
        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> ActualizarPregunta(Guid id, [FromBody] CrearPreguntaDto dto)
        {
            var userId = GetUserIdGuid();
            if (!userId.HasValue)
                return Unauthorized(new { ok = false, error = "Usuario no autenticado" });

            if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Cuerpo))
                return BadRequest(new { ok = false, error = "Título y cuerpo son requeridos" });

            var pregunta = await _db.Preguntas.FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);
            if (pregunta == null)
                return NotFound(new { ok = false, error = "Pregunta no encontrada" });

            if (pregunta.UsuarioId != userId.Value)
                return Forbid();

            try
            {
                pregunta.Titulo = dto.Titulo.Trim();
                pregunta.Cuerpo = dto.Cuerpo.Trim();
                pregunta.FechaModificacion = DateTimeOffset.UtcNow;

                // IMPORTANTE: NO regenerar slug al editar (mantener URLs estables)
                // El slug se genera solo al crear, no al editar
                // Si REALMENTE quieres actualizar el slug cuando cambia el título:
                // pregunta.Slug = await SlugHelper.GenerateUniqueSlugForPregunta(_db, dto.Titulo, id);

                _db.Preguntas.Update(pregunta);
                await _db.SaveChangesAsync();

                return Ok(new { ok = true, id = pregunta.Id, slug = pregunta.Slug });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar pregunta {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al actualizar la pregunta" });
            }
        }

        // POST api/preguntas/{id}/eliminar
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
                p.FechaModificacion = DateTimeOffset.UtcNow;
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
        [ValidateAntiForgeryToken]
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
                if (pregunta.Deshabilitado)
                    return BadRequest(new { ok = false, error = "No se puede votar esta pregunta (deshabilitada por moderación)." });

                if (pregunta.UsuarioId == userIdGuid)
                    return BadRequest("No puedes votar tu propia pregunta.");

                var existing = await _db.Votos.FirstOrDefaultAsync(v =>
                    v.EntidadTipo == VotoTipo.Pregunta && v.EntidadId == id && v.UsuarioId == userIdGuid);

                var votoEntityType = _db.Model.FindEntityType(typeof(Voto));
                bool hasFechaCreacion = votoEntityType?.FindProperty("FechaCreacion") != null;
                bool hasFechaModificacion = votoEntityType?.FindProperty("FechaModificacion") != null;

                if (existing == null)
                {
                    var voto = new Voto
                    {
                        Id = Guid.NewGuid(),
                        EntidadTipo = VotoTipo.Pregunta,
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
                    // Usuario ya votó previamente
                    if (existing.Valor == votoDto.Valor)
                    {
                        // Mismo valor: toggle activo ↔ cancelado
                        existing.Eliminado = !existing.Eliminado;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                    }
                    else
                    {
                        // CRÍTICO-02: cambio de dirección (ej: +1 → -1)
                        // 1) Marcar el voto anterior como eliminado
                        existing.Eliminado = true;
                        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
                        _db.Votos.Update(existing);
                        // 2) Crear el nuevo voto con el nuevo valor
                        var nuevoVoto = new Voto
                        {
                            Id = Guid.NewGuid(),
                            EntidadTipo = VotoTipo.Pregunta,
                            EntidadId = id,
                            UsuarioId = userIdGuid,
                            Valor = (short)votoDto.Valor,
                            Eliminado = false
                        };
                        if (hasFechaCreacion) nuevoVoto.FechaCreacion = DateTimeOffset.UtcNow;
                        _db.Votos.Add(nuevoVoto);
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
                        _logger.LogWarning(dbEx, "Unique constraint violation while saving vote for pregunta {Id}; will re-read score/userVote.", id);
                    }
                    else
                    {
                        throw;
                    }
                }

                var score = await _db.Votos
                    .Where(v => v.EntidadTipo == VotoTipo.Pregunta && v.EntidadId == id && !v.Eliminado)
                    .Select(v => (int?)v.Valor).SumAsync() ?? 0;

                var votoActual = await _db.Votos
                    .Where(v => v.EntidadTipo == VotoTipo.Pregunta && v.EntidadId == id && v.UsuarioId == userIdGuid)
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

        // ===== NUEVO: GET api/preguntas/{id}/ai-status - Verificar estado de respuesta IA =====
        /// <summary>
        /// Endpoint para polling: verifica si ya existe respuesta de IA para una pregunta
        /// </summary>
        [HttpGet("{id:guid}/ai-status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAiStatus(Guid id)
        {
            try
            {
                var pregunta = await _db.Preguntas.AsNoTracking()
                    .Where(p => p.Id == id && !p.Eliminado)
                    .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
                    .FirstOrDefaultAsync();

                if (pregunta == null)
                    return NotFound(new { ok = false, error = "Pregunta no encontrada" });

                bool hasAiAnswer = pregunta.TieneRespuestaIA;
                DateTimeOffset? generatedAt = pregunta.FechaGeneracionIA;

                return Ok(new
                {
                    ok = true,
                    hasAiAnswer,
                    generatedAt,
                    // Si tiene respuesta, también podríamos devolver el ID para navegación
                    respuestaId = hasAiAnswer
                        ? (await _db.Respuestas.AsNoTracking()
                            .Where(r => r.PreguntaId == id && !r.Eliminado && r.EsIA)
                            .Select(r => (Guid?)r.Id)
                            .FirstOrDefaultAsync())
                        : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando estado AI para pregunta {Id}", id);
                return StatusCode(500, new { ok = false, error = "Error al verificar estado" });
            }
        }
        // ==================================================================================
    }
}