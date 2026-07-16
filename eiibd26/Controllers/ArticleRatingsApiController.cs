using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System.Security.Claims;

namespace eiibd26.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticleRatingsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ArticleRatingsApiController> _logger;

    public ArticleRatingsApiController(
        ApplicationDbContext context,
        ILogger<ArticleRatingsApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtener las estadísticas de rating de un artículo
    /// GET /api/articles/{articleId}/rating
    /// </summary>
    [HttpGet("{articleId:int}/rating")]
    public async Task<IActionResult> GetRatingStats(int articleId)
    {
        try
        {
            // Verificar que el artículo existe
            var articleExists = await _context.Contenidos
                .AnyAsync(c => c.Id == articleId && !c.Eliminado);

            if (!articleExists)
            {
                return NotFound(new { ok = false, error = "Artículo no encontrado" });
            }

            // Obtener estadísticas
            var ratings = await _context.ArticleRatings
                .Where(ar => ar.ArticleId == articleId)
                .ToListAsync();

            var likes = ratings.Count(r => r.RatingType == RatingType.Like);
            var dislikes = ratings.Count(r => r.RatingType == RatingType.Dislike);

            // Verificar si el usuario actual ya votó
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ArticleRating? userRating = null;

            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                userRating = ratings.FirstOrDefault(r => r.UserId == userGuid);
            }

            return Ok(new
            {
                ok = true,
                estadisticas = new
                {
                    likes,
                    dislikes,
                    total = likes + dislikes
                },
                ratingUsuario = userRating != null ? new
                {
                    tipo = userRating.RatingType.ToString().ToLower(),
                    fecha = userRating.CreatedAt
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas de rating para artículo {ArticleId}", articleId);
            return StatusCode(500, new { ok = false, error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Registrar o actualizar un rating de un artículo
    /// POST /api/articles/{articleId}/rating
    /// Body: { "ratingType": "like" | "dislike" }
    /// </summary>
    [HttpPost("{articleId:int}/rating")]
    public async Task<IActionResult> RateArticle(int articleId, [FromBody] RateArticleRequest request)
    {
        try
        {
            // Validar request
            if (request == null || string.IsNullOrWhiteSpace(request.RatingType))
            {
                return BadRequest(new { ok = false, error = "Tipo de calificación requerido" });
            }

            // Parse rating type
            if (!Enum.TryParse<RatingType>(request.RatingType, true, out var ratingType))
            {
                return BadRequest(new { ok = false, error = "Tipo de calificación inválido. Use 'like' o 'dislike'" });
            }

            // M-4: solo usuarios autenticados. El voto anónimo permitía inflar o hundir el
            // rating de contenido médico con un loop de curl. Va antes de comprobar que el
            // artículo existe para no responder sobre su existencia a quien no tiene sesión.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized(new { ok = false, error = "Inicia sesión para calificar" });

            // Verificar que el artículo existe
            var articleExists = await _context.Contenidos
                .AnyAsync(c => c.Id == articleId && !c.Eliminado);

            if (!articleExists)
            {
                return NotFound(new { ok = false, error = "Artículo no encontrado" });
            }

            var ipAddress = GetClientIpAddress();

            // Un voto por usuario y artículo. La deduplicación por IP de la rama anónima
            // desaparece con ella: la identidad ya la da la sesión.
            var existing = await _context.ArticleRatings
                .FirstOrDefaultAsync(ar => ar.ArticleId == articleId && ar.UserId == userGuid);

            if (existing != null)
            {
                // Actualizar voto existente
                existing.RatingType = ratingType;
                existing.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Usuario {UserId} actualizó rating de artículo {ArticleId} a {RatingType}",
                    userId, articleId, ratingType);
            }
            else
            {
                // Crear nuevo voto
                var newRating = new ArticleRating
                {
                    ArticleId = articleId,
                    RatingType = ratingType,
                    UserId = userGuid,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ArticleRatings.Add(newRating);

                _logger.LogInformation("Usuario {UserId} creó rating de artículo {ArticleId}: {RatingType}",
                    userId, articleId, ratingType);
            }

            await _context.SaveChangesAsync();

            // Retornar estadísticas actualizadas
            var stats = await GetUpdatedStats(articleId);

            return Ok(new
            {
                ok = true,
                message = existing != null ? "Calificación actualizada" : "Calificación registrada",
                estadisticas = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar rating para artículo {ArticleId}", articleId);
            return StatusCode(500, new { ok = false, error = "Error al guardar calificación" });
        }
    }

    /// <summary>
    /// Obtener IP del cliente
    /// </summary>
    // SEC-007: Se usa RemoteIpAddress (IP real de la conexión TCP) como identificador de deduplicación.
    // X-Forwarded-For se ignora deliberadamente porque puede ser falsificado por el cliente.
    // Si la app se coloca detrás de un reverse proxy confiable, configurar UseForwardedHeaders en Program.cs
    // y sólo entonces leer HttpContext.Connection.RemoteIpAddress (que ya será la IP desenvuelta por el middleware).
    private string? GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// Obtener estadísticas actualizadas de un artículo
    /// </summary>
    private async Task<object> GetUpdatedStats(int articleId)
    {
        var ratings = await _context.ArticleRatings
            .Where(ar => ar.ArticleId == articleId)
            .Select(ar => ar.RatingType)
            .ToListAsync();

        var likes = ratings.Count(r => r == RatingType.Like);
        var dislikes = ratings.Count(r => r == RatingType.Dislike);

        return new
        {
            likes,
            dislikes,
            total = likes + dislikes
        };
    }
}

/// <summary>
/// Request para calificar un artículo
/// </summary>
public class RateArticleRequest
{
    /// <summary>
    /// Tipo de calificación: "like" o "dislike"
    /// </summary>
    public string RatingType { get; set; } = string.Empty;
}
