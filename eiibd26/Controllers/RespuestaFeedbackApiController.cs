using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/respuestas")]
    [Authorize] // Solo usuarios autenticados pueden dar feedback
    public class RespuestaFeedbackApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RespuestaFeedbackApiController> _logger;

        public RespuestaFeedbackApiController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<RespuestaFeedbackApiController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/respuestas/{respuestaId}/feedback
        /// Dar feedback (like/dislike) a una respuesta de IA
        /// </summary>
        [HttpPost("{respuestaId:guid}/feedback")]
        public async Task<IActionResult> DarFeedback(
            Guid respuestaId,
            [FromBody] FeedbackRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar que la respuesta existe y es de IA
                var respuesta = await _db.Respuestas
                    .Where(r => r.Id == respuestaId && r.EsIA && !r.Eliminado)
                    .FirstOrDefaultAsync(cancellationToken);

                if (respuesta == null)
                {
                    return NotFound(new { ok = false, error = "Respuesta de IA no encontrada" });
                }

                // Obtener usuario actual
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { ok = false, error = "Usuario no autenticado" });
                }

                _logger.LogInformation(
                    "👍/👎 [Feedback] Usuario {UserId} dando feedback a respuesta {RespuestaId}: {EsUtil}",
                    user.Id, respuestaId, request.EsUtil);

                // Buscar si ya existe feedback de este usuario para esta respuesta
                var feedbackExistente = await _db.RespuestaAIFeedbacks
                    .Where(f => f.RespuestaId == respuestaId && f.UsuarioId == user.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (feedbackExistente != null)
                {
                    // Actualizar feedback existente
                    _logger.LogInformation(
                        "🔄 [Feedback] Actualizando feedback existente {FeedbackId}",
                        feedbackExistente.Id);

                    feedbackExistente.EsUtil = request.EsUtil;
                    feedbackExistente.Comentario = request.Comentario?.Trim();
                    feedbackExistente.FechaCreacion = DateTimeOffset.UtcNow;
                }
                else
                {
                    // Crear nuevo feedback
                    var nuevoFeedback = new RespuestaAIFeedback
                    {
                        RespuestaId = respuestaId,
                        UsuarioId = user.Id,
                        EsUtil = request.EsUtil,
                        Comentario = request.Comentario?.Trim(),
                        FechaCreacion = DateTimeOffset.UtcNow
                    };

                    _db.RespuestaAIFeedbacks.Add(nuevoFeedback);

                    _logger.LogInformation(
                        "✨ [Feedback] Nuevo feedback creado para respuesta {RespuestaId}",
                        respuestaId);
                }

                await _db.SaveChangesAsync(cancellationToken);

                // Obtener estadísticas actualizadas
                var stats = await ObtenerEstadisticas(respuestaId, cancellationToken);

                _logger.LogInformation(
                    "✅ [Feedback] Guardado exitoso. Stats: {Likes} likes, {Dislikes} dislikes",
                    stats.Likes, stats.Dislikes);

                return Ok(new
                {
                    ok = true,
                    message = "Feedback guardado correctamente",
                    estadisticas = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Feedback] Error al guardar feedback");
                return StatusCode(500, new { ok = false, error = "Error al guardar feedback" });
            }
        }

        /// <summary>
        /// GET /api/respuestas/{respuestaId}/feedback
        /// Obtener estadísticas de feedback de una respuesta
        /// </summary>
        [HttpGet("{respuestaId:guid}/feedback")]
        [AllowAnonymous] // Estadísticas son públicas
        public async Task<IActionResult> ObtenerFeedback(
            Guid respuestaId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await ObtenerEstadisticas(respuestaId, cancellationToken);

                // Si el usuario está autenticado, incluir su feedback
                FeedbackUsuario? feedbackUsuario = null;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var feedback = await _db.RespuestaAIFeedbacks
                            .Where(f => f.RespuestaId == respuestaId && f.UsuarioId == user.Id)
                            .Select(f => new FeedbackUsuario
                            {
                                EsUtil = f.EsUtil,
                                Comentario = f.Comentario,
                                FechaCreacion = f.FechaCreacion
                            })
                            .FirstOrDefaultAsync(cancellationToken);

                        feedbackUsuario = feedback;
                    }
                }

                return Ok(new
                {
                    ok = true,
                    estadisticas = stats,
                    feedbackUsuario = feedbackUsuario
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Feedback] Error al obtener feedback");
                return StatusCode(500, new { ok = false, error = "Error al obtener feedback" });
            }
        }

        /// <summary>
        /// DELETE /api/respuestas/{respuestaId}/feedback
        /// Eliminar feedback del usuario actual
        /// </summary>
        [HttpDelete("{respuestaId:guid}/feedback")]
        public async Task<IActionResult> EliminarFeedback(
            Guid respuestaId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { ok = false, error = "Usuario no autenticado" });
                }

                var feedback = await _db.RespuestaAIFeedbacks
                    .Where(f => f.RespuestaId == respuestaId && f.UsuarioId == user.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (feedback == null)
                {
                    return NotFound(new { ok = false, error = "Feedback no encontrado" });
                }

                _db.RespuestaAIFeedbacks.Remove(feedback);
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "🗑️ [Feedback] Usuario {UserId} eliminó su feedback de respuesta {RespuestaId}",
                    user.Id, respuestaId);

                return Ok(new { ok = true, message = "Feedback eliminado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Feedback] Error al eliminar feedback");
                return StatusCode(500, new { ok = false, error = "Error al eliminar feedback" });
            }
        }

        // ===== MÉTODOS AUXILIARES =====

        private async Task<EstadisticasFeedback> ObtenerEstadisticas(
            Guid respuestaId,
            CancellationToken cancellationToken)
        {
            var feedbacks = await _db.RespuestaAIFeedbacks
                .Where(f => f.RespuestaId == respuestaId)
                .GroupBy(f => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Likes = g.Count(f => f.EsUtil),
                    Dislikes = g.Count(f => !f.EsUtil)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (feedbacks == null)
            {
                return new EstadisticasFeedback
                {
                    Total = 0,
                    Likes = 0,
                    Dislikes = 0,
                    PorcentajeLikes = 0
                };
            }

            var porcentaje = feedbacks.Total > 0
                ? (double)feedbacks.Likes / feedbacks.Total * 100
                : 0;

            return new EstadisticasFeedback
            {
                Total = feedbacks.Total,
                Likes = feedbacks.Likes,
                Dislikes = feedbacks.Dislikes,
                PorcentajeLikes = Math.Round(porcentaje, 1)
            };
        }
    }

    // ===== MODELOS DE REQUEST/RESPONSE =====

    public class FeedbackRequest
    {
        public bool EsUtil { get; set; }
        public string? Comentario { get; set; }
    }

    public class EstadisticasFeedback
    {
        public int Total { get; set; }
        public int Likes { get; set; }
        public int Dislikes { get; set; }
        public double PorcentajeLikes { get; set; }
    }

    public class FeedbackUsuario
    {
        public bool EsUtil { get; set; }
        public string? Comentario { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
    }
}
