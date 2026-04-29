using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Controllers;

/// <summary>
/// Endpoint público para registrar mood desde notificaciones push
/// sin requerir sesión activa. Autenticación mediante token de corta duración.
/// </summary>
[ApiController]
[Route("api/mood")]
public class MoodApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPushMoodTokenService _tokenService;

    public MoodApiController(ApplicationDbContext db, IPushMoodTokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    /// <summary>
    /// POST /api/mood/quick?token=&amp;valor=
    /// Registra un estado de ánimo usando un token de un solo uso (5 min).
    /// No requiere cookie ni sesión activa.
    /// </summary>
    [HttpPost("quick")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> QuickMood(
        [FromQuery] string token,
        [FromQuery] int valor,
        CancellationToken ct = default)
    {
        var userId = _tokenService.ValidarToken(token);
        if (userId is null)
            return Unauthorized(new { ok = false, error = "Token inválido o expirado." });

        if (!Enum.IsDefined(typeof(EstadoAnimoEnum), valor))
            return BadRequest(new { ok = false, error = "Valor de mood fuera de rango (1-5)." });

        // Evitar duplicados en la misma ventana de 5 min (idempotencia)
        var ventana = DateTime.UtcNow.AddMinutes(-5);
        var yaRegistrado = await _db.EstadoAnimoUsuario
            .AnyAsync(e => e.IdUsuario == userId.Value
                        && e.FechaRegistro >= ventana
                        && !e.Eliminado, ct);

        if (yaRegistrado)
            return Ok(new { ok = true, duplicado = true });

        var estado = new EstadoAnimoUsuario
        {
            IdUsuario = userId.Value,
            EstadoMood = (EstadoAnimoEnum)valor,
            FechaRegistro = DateTime.UtcNow
        };

        _db.EstadoAnimoUsuario.Add(estado);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = estado.Id });
    }
}
