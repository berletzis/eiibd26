using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models.Platillos;
using System.Security.Claims;

namespace eiibd26.Controllers;

/// <summary>
/// Calificación de utilidad GENÉRICA de un destino del módulo (platillo o ingrediente).
/// Espejo de ArticleRatingsApiController pero contra su PROPIA tabla polimórfica
/// PlatCalificacion (TipoDestino+DestinoId) — NUNCA toca ArticleRating (esa se llavea
/// por ContenidoId y mezclaría votos de ingrediente #5 con artículo #5).
/// Solo usuarios autenticados votan. Response likes/dislikes para reusar el mismo JS.
/// </summary>
[ApiController]
[Route("api/platillos/calificacion")]
public class PlatCalificacionesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PlatCalificacionesApiController> _logger;

    public PlatCalificacionesApiController(
        ApplicationDbContext context,
        ILogger<PlatCalificacionesApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private const short ValorUtil = 1;    // like
    private const short ValorNoUtil = -1; // dislike

    // Normaliza el tipo a la forma canónica de la BD ('Ingrediente' | 'Platillo'), o null si inválido.
    private static string? NormalizarTipo(string? tipo)
    {
        if (string.IsNullOrWhiteSpace(tipo)) return null;
        if (tipo.Equals("Ingrediente", StringComparison.OrdinalIgnoreCase)) return "Ingrediente";
        if (tipo.Equals("Platillo", StringComparison.OrdinalIgnoreCase)) return "Platillo";
        return null;
    }

    private async Task<bool> DestinoExisteAsync(string tipo, int destinoId) => tipo switch
    {
        "Ingrediente" => await _context.PlatIngredientes.AsNoTracking().AnyAsync(i => i.Id == destinoId && i.Activo),
        "Platillo" => await _context.PlatPlatillos.AsNoTracking().AnyAsync(p => p.Id == destinoId && p.Activo),
        _ => false
    };

    /// <summary>GET /api/platillos/calificacion/{tipoDestino}/{destinoId}</summary>
    [HttpGet("{tipoDestino}/{destinoId:int}")]
    public async Task<IActionResult> GetStats(string tipoDestino, int destinoId)
    {
        try
        {
            var tipo = NormalizarTipo(tipoDestino);
            if (tipo == null) return BadRequest(new { ok = false, error = "Tipo de destino inválido" });

            if (!await DestinoExisteAsync(tipo, destinoId))
                return NotFound(new { ok = false, error = "Destino no encontrado" });

            var votos = await _context.PlatCalificaciones.AsNoTracking()
                .Where(c => c.TipoDestino == tipo && c.DestinoId == destinoId)
                .Select(c => new { c.Valor, c.idUsuario })
                .ToListAsync();

            var likes = votos.Count(v => v.Valor == ValorUtil);
            var dislikes = votos.Count(v => v.Valor == ValorNoUtil);

            object? ratingUsuario = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var uid))
            {
                var mio = votos.FirstOrDefault(v => v.idUsuario == uid);
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
            _logger.LogError(ex, "Error al obtener calificación {Tipo}/{DestinoId}", tipoDestino, destinoId);
            return StatusCode(500, new { ok = false, error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// POST /api/platillos/calificacion/{tipoDestino}/{destinoId}
    /// Body: { "ratingType": "like" | "dislike" }. Requiere sesión.
    /// </summary>
    [HttpPost("{tipoDestino}/{destinoId:int}")]
    public async Task<IActionResult> Rate(string tipoDestino, int destinoId, [FromBody] RatePlatCalificacionRequest request)
    {
        try
        {
            var tipo = NormalizarTipo(tipoDestino);
            if (tipo == null) return BadRequest(new { ok = false, error = "Tipo de destino inválido" });

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

            if (!await DestinoExisteAsync(tipo, destinoId))
                return NotFound(new { ok = false, error = "Destino no encontrado" });

            var existente = await _context.PlatCalificaciones
                .FirstOrDefaultAsync(c => c.TipoDestino == tipo && c.DestinoId == destinoId && c.idUsuario == uid);

            if (existente != null)
            {
                existente.Valor = valor;
                existente.Fecha = DateTime.UtcNow;
            }
            else
            {
                _context.PlatCalificaciones.Add(new PlatCalificacion
                {
                    TipoDestino = tipo,
                    DestinoId = destinoId,
                    idUsuario = uid,
                    Valor = valor,
                    Fecha = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var votos = await _context.PlatCalificaciones.AsNoTracking()
                .Where(c => c.TipoDestino == tipo && c.DestinoId == destinoId)
                .Select(c => c.Valor)
                .ToListAsync();
            var likes = votos.Count(v => v == ValorUtil);
            var dislikes = votos.Count(v => v == ValorNoUtil);

            return Ok(new
            {
                ok = true,
                message = existente != null ? "Calificación actualizada" : "Calificación registrada",
                estadisticas = new { likes, dislikes, total = likes + dislikes }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar calificación {Tipo}/{DestinoId}", tipoDestino, destinoId);
            return StatusCode(500, new { ok = false, error = "Error al guardar calificación" });
        }
    }
}

public class RatePlatCalificacionRequest
{
    public string RatingType { get; set; } = string.Empty;
}
