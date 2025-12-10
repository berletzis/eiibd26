using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eiibd26.Controllers
{
    [IgnoreAntiforgeryToken]
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EstadoAnimoUsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public EstadoAnimoUsuarioController(ApplicationDbContext db) { _db = db; }

        [HttpGet("historico")]
        public async Task<ActionResult<List<object>>> Historico()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var lista = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid && !x.Eliminado)
                .OrderByDescending(x => x.FechaRegistro)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Include(x => x.SintomaUsuario).ThenInclude(su => su.Sintoma)
                .Include(x => x.TratamientoUsuario).ThenInclude(tu => tu.Tratamiento)
                .Select(x => new
                {
                    Id = x.Id,
                    EstadoMood = x.EstadoMood,
                    Texto = x.Texto,
                    // Enviar la fecha como string ISO (ISO 8601) para evitar ambigüedades de zona/parseo en el cliente
                    FechaRegistro = x.FechaRegistro.ToString("o"),
                    RelacionNombre = x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre
                                   : x.SintomaUsuario != null ? x.SintomaUsuario.Sintoma.nombre
                                   : x.TratamientoUsuario != null ? x.TratamientoUsuario.Tratamiento.nombre
                                   : null,
                    TipoRelacion = x.CondicionUsuario != null ? "Condicion"
                                 : x.SintomaUsuario != null ? "Sintoma"
                                 : x.TratamientoUsuario != null ? "Tratamiento"
                                 : null
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("condiciones-usuario")]
        public async Task<ActionResult<List<object>>> CondicionesUsuario()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var condiciones = await _db.condicionUsuario
                .Where(x => x.idUsuario == guid && !x.Eliminado)
                .Include(x => x.Condicion)
                .Select(x => new { id = x.id, nombre = x.Condicion.nombre })
                .ToListAsync();

            return Ok(condiciones);
        }

        [HttpPost("nuevo")]
        public async Task<ActionResult<object>> Nuevo([FromForm] string mood, [FromForm] string? texto, [FromForm] int? condicionUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(mood))
                return BadRequest(new { ok = false, error = "El campo mood es requerido." });

            // Opcional: validar valores permitidos para EstadoMood
            var allowed = new[] { "MuyBien", "Bien", "Neutral", "Mal", "MuyMal" };
            if (!allowed.Contains(mood))
                return BadRequest(new { ok = false, error = "Valor de mood inválido." });

            if (string.IsNullOrWhiteSpace(texto)) texto = null;

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = guid,
                EstadoMood = mood,
                Texto = texto,
                // Usar UTC para consistencia en la API y serialización
                FechaRegistro = DateTime.UtcNow,
                IdCondicionUsuario = condicionUsuarioId
            };

            _db.EstadoAnimoUsuario.Add(nuevo);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // En un proyecto real aquí sería mejor loggear el error.
                return StatusCode(500, new { ok = false, error = "Error al guardar el estado de ánimo." });
            }

            string nombre = null; string tipo = null;
            if (condicionUsuarioId.HasValue)
            {
                nombre = await _db.condicionUsuario
                    .Include(c => c.Condicion)
                    .Where(c => c.id == condicionUsuarioId)
                    .Select(c => c.Condicion.nombre)
                    .FirstOrDefaultAsync();
                tipo = "Condicion";
            }

            return Ok(new
            {
                EstadoMood = nuevo.EstadoMood,
                Texto = nuevo.Texto,
                // devolver fecha en ISO para que el cliente la parsee sin ambigüedad
                FechaRegistro = nuevo.FechaRegistro.ToString("o"),
                RelacionNombre = nombre,
                TipoRelacion = tipo
            });
        }

        [HttpPost("eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var estado = await _db.EstadoAnimoUsuario.FirstOrDefaultAsync(e => e.Id == id && e.IdUsuario == guid);
            if (estado == null) return NotFound(new { ok = false, error = "Registro no encontrado." });

            estado.Eliminado = true;
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                return StatusCode(500, new { ok = false, error = "Error al eliminar el estado de ánimo." });
            }

            return Ok(new { ok = true });
        }
    }
}