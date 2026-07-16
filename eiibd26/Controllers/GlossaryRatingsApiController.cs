using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using eiibd26.Models.Glossary;
using System.Security.Claims;

namespace eiibd26.Controllers;

[ApiController]
[Route("api/glossary")]
public class GlossaryRatingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GlossaryRatingsApiController> _logger;

    public GlossaryRatingsApiController(
        ApplicationDbContext context,
        ILogger<GlossaryRatingsApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtener estadísticas de rating de un término del glosario.
    /// GET /api/glossary/{termId}/rating
    /// </summary>
    [HttpGet("{termId:int}/rating")]
    public async Task<IActionResult> GetRatingStats(int termId)
    {
        try
        {
            var termExists = await _context.GlossaryTerms
                .AnyAsync(t => t.Id == termId && t.Activo);

            if (!termExists)
                return NotFound(new { ok = false, error = "Término no encontrado" });

            var ratings = await _context.GlossaryTermRatings
                .Where(r => r.TermId == termId)
                .ToListAsync();

            var likes = ratings.Count(r => r.RatingType == RatingType.Like);
            var dislikes = ratings.Count(r => r.RatingType == RatingType.Dislike);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            GlossaryTermRating? userRating = null;

            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
                userRating = ratings.FirstOrDefault(r => r.UserId == userGuid);

            return Ok(new
            {
                ok = true,
                estadisticas = new { likes, dislikes, total = likes + dislikes },
                ratingUsuario = userRating != null ? new
                {
                    tipo = userRating.RatingType.ToString().ToLower(),
                    fecha = userRating.CreatedAt
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de rating para término {TermId}", termId);
            return StatusCode(500, new { ok = false, error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Registrar o actualizar un rating de un término del glosario.
    /// POST /api/glossary/{termId}/rating
    /// Body: { "ratingType": "like" | "dislike" }
    /// </summary>
    [HttpPost("{termId:int}/rating")]
    public async Task<IActionResult> RateTerm(int termId, [FromBody] RateTermRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RatingType))
                return BadRequest(new { ok = false, error = "Tipo de calificación requerido" });

            if (!Enum.TryParse<RatingType>(request.RatingType, true, out var ratingType))
                return BadRequest(new { ok = false, error = "Tipo de calificación inválido. Use 'like' o 'dislike'" });

            // M-4: solo usuarios autenticados. El voto anónimo permitía inflar o hundir el
            // rating de contenido médico con un loop de curl. Va antes de comprobar que el
            // término existe para no responder sobre su existencia a quien no tiene sesión.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { ok = false, error = "Inicia sesión para calificar" });

            var termExists = await _context.GlossaryTerms
                .AnyAsync(t => t.Id == termId && t.Activo);

            if (!termExists)
                return NotFound(new { ok = false, error = "Término no encontrado" });

            var ipAddress = GetClientIpAddress();

            // Un voto por usuario y término. La deduplicación por IP de la rama anónima
            // desaparece con ella: la identidad ya la da la sesión.
            var existing = await _context.GlossaryTermRatings
                .FirstOrDefaultAsync(r => r.TermId == termId && r.UserId == userGuid);

            if (existing != null)
            {
                existing.RatingType = ratingType;
                existing.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Usuario {UserId} actualizó rating de término {TermId} a {RatingType}",
                    userId, termId, ratingType);
            }
            else
            {
                var newRating = new GlossaryTermRating
                {
                    TermId = termId,
                    RatingType = ratingType,
                    UserId = userGuid,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GlossaryTermRatings.Add(newRating);
                _logger.LogInformation("Usuario {UserId} creó rating de término {TermId}: {RatingType}",
                    userId, termId, ratingType);
            }

            await _context.SaveChangesAsync();

            var stats = await GetUpdatedStats(termId);

            return Ok(new
            {
                ok = true,
                message = existing != null ? "Calificación actualizada" : "Calificación registrada",
                estadisticas = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar rating para término {TermId}", termId);
            return StatusCode(500, new { ok = false, error = "Error al guardar calificación" });
        }
    }

    // SEC-007: Se usa RemoteIpAddress (IP real de la conexión TCP) como identificador de deduplicación.
    // Ver comentario en ArticleRatingsApiController.GetClientIpAddress.
    private string? GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task<object> GetUpdatedStats(int termId)
    {
        var ratings = await _context.GlossaryTermRatings
            .Where(r => r.TermId == termId)
            .Select(r => r.RatingType)
            .ToListAsync();

        var likes = ratings.Count(r => r == RatingType.Like);
        var dislikes = ratings.Count(r => r == RatingType.Dislike);

        return new { likes, dislikes, total = likes + dislikes };
    }
}

/// <summary>Request para calificar un término del glosario.</summary>
public class RateTermRequest
{
    /// <summary>Tipo de calificación: "like" o "dislike"</summary>
    public string RatingType { get; set; } = string.Empty;
}
