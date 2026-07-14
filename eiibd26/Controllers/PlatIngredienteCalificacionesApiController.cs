using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models.Platillos;
using System.Security.Claims;

namespace eiibd26.Controllers;

/// <summary>
/// Calificación de utilidad de un ingrediente ("¿Puedo comer X?"). Espejo de
/// ArticleRatingsApiController pero contra su PROPIA tabla PlatIngredienteCalificacion
/// (NO reusa ArticleRating). Solo usuarios autenticados votan; anónimo ve el aviso de
/// "inicia sesión" en la vista y este endpoint no crea votos anónimos.
/// Response con forma likes/dislikes para reusar el mismo JS del rating de artículos.
/// </summary>
[ApiController]
[Route("api/platillos/ingredientes")]
public class PlatIngredienteCalificacionesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlatIngredienteCalificacionesApiController> _logger;

    public PlatIngredienteCalificacionesApiController(
        ApplicationDbContext context,
        ILogger<PlatIngredienteCalificacionesApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1 = me fue útil (like) · -1 = no me fue útil (dislike)
    private const short ValorUtil = 1;
    private const short ValorNoUtil = -1;

    /// <summary>GET /api/platillos/ingredientes/{ingredienteId}/calificacion</summary>
    [HttpGet("{ingredienteId:int}/calificacion")]
    public async Task<IActionResult> GetStats(int ingredienteId)
    {
        try
        {
            var existe = await _context.PlatIngredientes.AsNoTracking()
                .AnyAsync(i => i.Id == ingredienteId && i.Activo);
            if (!existe)
                return NotFound(new { ok = false, error = "Ingrediente no encontrado" });

            var valores = await _context.PlatIngredienteCalificaciones.AsNoTracking()
                .Where(c => c.IngredienteId == ingredienteId)
                .Select(c => new { c.Valor, c.idUsuario })
                .ToListAsync();

            var likes = valores.Count(v => v.Valor == ValorUtil);
            var dislikes = valores.Count(v => v.Valor == ValorNoUtil);

            object? ratingUsuario = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid))
            {
                var mio = valores.FirstOrDefault(v => v.idUsuario == uid);
                if (mio != null)
                    ratingUsuario = new { tipo = mio.Valor == ValorUtil ? "like" : "dislike" };
            }

            return Ok(new
            {
                ok = true,
                estadisticas = new { likes, dislikes, total = likes + dislikes },
                ratingUsuario
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener calificación de ingrediente {IngredienteId}", ingredienteId);
            return StatusCode(500, new { ok = false, error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// POST /api/platillos/ingredientes/{ingredienteId}/calificacion
    /// Body: { "ratingType": "like" | "dislike" }. Requiere sesión.
    /// </summary>
    [HttpPost("{ingredienteId:int}/calificacion")]
    public async Task<IActionResult> Rate(int ingredienteId, [FromBody] RatePlatIngredienteRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RatingType))
                return BadRequest(new { ok = false, error = "Tipo de calificación requerido" });

            short valor;
            if (request.RatingType.Equals("like", StringComparison.OrdinalIgnoreCase)) valor = ValorUtil;
            else if (request.RatingType.Equals("dislike", StringComparison.OrdinalIgnoreCase)) valor = ValorNoUtil;
            else return BadRequest(new { ok = false, error = "Tipo inválido. Use 'like' o 'dislike'" });

            // Solo usuarios autenticados: no hay voto anónimo (la vista muestra "inicia sesión").
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var uid))
                return Unauthorized(new { ok = false, error = "Inicia sesión para calificar" });

            var existe = await _context.PlatIngredientes.AsNoTracking()
                .AnyAsync(i => i.Id == ingredienteId && i.Activo);
            if (!existe)
                return NotFound(new { ok = false, error = "Ingrediente no encontrado" });

            var existente = await _context.PlatIngredienteCalificaciones
                .FirstOrDefaultAsync(c => c.IngredienteId == ingredienteId && c.idUsuario == uid);

            if (existente != null)
            {
                existente.Valor = valor;
                existente.Fecha = DateTime.UtcNow;
            }
            else
            {
                _context.PlatIngredienteCalificaciones.Add(new PlatIngredienteCalificacion
                {
                    IngredienteId = ingredienteId,
                    idUsuario = uid,
                    Valor = valor,
                    Fecha = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var valores = await _context.PlatIngredienteCalificaciones.AsNoTracking()
                .Where(c => c.IngredienteId == ingredienteId)
                .Select(c => c.Valor)
                .ToListAsync();
            var likes = valores.Count(v => v == ValorUtil);
            var dislikes = valores.Count(v => v == ValorNoUtil);

            return Ok(new
            {
                ok = true,
                message = existente != null ? "Calificación actualizada" : "Calificación registrada",
                estadisticas = new { likes, dislikes, total = likes + dislikes }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar calificación de ingrediente {IngredienteId}", ingredienteId);
            return StatusCode(500, new { ok = false, error = "Error al guardar calificación" });
        }
    }
}

public class RatePlatIngredienteRequest
{
    public string RatingType { get; set; } = string.Empty;
}
