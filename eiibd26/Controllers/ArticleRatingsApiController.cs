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

            // Verificar que el artículo existe
            var articleExists = await _context.Contenidos
                .AnyAsync(c => c.Id == articleId && !c.Eliminado);

            if (!articleExists)
            {
                return NotFound(new { ok = false, error = "Artículo no encontrado" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ipAddress = GetClientIpAddress();
            ArticleRating? existing = null;

            // Buscar rating existente
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                // Usuario autenticado: buscar por UserId
                existing = await _context.ArticleRatings
                    .FirstOrDefaultAsync(ar => ar.ArticleId == articleId && ar.UserId == userGuid);
            }
            else if (!string.IsNullOrEmpty(ipAddress))
            {
                // Usuario anónimo: buscar por IP (últimas 24 horas para evitar conflictos)
                var yesterday = DateTime.UtcNow.AddDays(-1);
                existing = await _context.ArticleRatings
                    .Where(ar => ar.ArticleId == articleId 
                              && ar.UserId == null 
                              && ar.IpAddress == ipAddress
                              && ar.CreatedAt >= yesterday)
                    .OrderByDescending(ar => ar.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            if (existing != null)
            {
                // Actualizar voto existente
                existing.RatingType = ratingType;
                existing.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Usuario {UserId} actualizó rating de artículo {ArticleId} a {RatingType}",
                    userId ?? "anónimo", articleId, ratingType);
            }
            else
            {
                // Crear nuevo voto
                var newRating = new ArticleRating
                {
                    ArticleId = articleId,
                    RatingType = ratingType,
                    UserId = !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var guid) ? guid : null,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ArticleRatings.Add(newRating);

                _logger.LogInformation("Usuario {UserId} creó rating de artículo {ArticleId}: {RatingType}",
                    userId ?? "anónimo", articleId, ratingType);
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
    private string? GetClientIpAddress()
    {
        try
        {
            // Intentar obtener IP de headers de proxy
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            // IP directa
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

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
