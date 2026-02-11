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

            // ✅ CAMBIO: Cargar datos primero SIN proyección
            var registros = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid && !x.Eliminado)
                .OrderByDescending(x => x.FechaRegistro)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Include(x => x.SintomaUsuario).ThenInclude(su => su.Sintoma)
                .Include(x => x.TratamientoUsuario).ThenInclude(tu => tu.Tratamiento)
                .AsNoTracking()
                .ToListAsync();

            // ✅ CAMBIO: Proyectar EN MEMORIA (no en SQL)
            var lista = registros.Select(x => new
            {
                Id = x.Id,
                EstadoMood = (int)x.EstadoMood,
                EstadoMoodNombre = x.EstadoMood.ToString(),
                Texto = x.Texto,
                FechaRegistro = x.FechaRegistro.ToString("o"),
                RelacionNombre = x.CondicionUsuario?.Condicion?.nombre
                               ?? x.SintomaUsuario?.Sintoma?.nombre
                               ?? x.TratamientoUsuario?.Tratamiento?.nombre,
                TipoRelacion = x.CondicionUsuario != null ? "Condicion"
                             : x.SintomaUsuario != null ? "Sintoma"
                             : x.TratamientoUsuario != null ? "Tratamiento"
                             : null
            }).ToList();

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

            EstadoAnimoEnum estadoEnum;

            // Intentar parsear como número (1-5)
            if (int.TryParse(mood, out int moodNumero))
            {
                if (moodNumero < 1 || moodNumero > 5)
                    return BadRequest(new { ok = false, error = "Valor de mood debe estar entre 1 y 5." });

                estadoEnum = (EstadoAnimoEnum)moodNumero;
            }
            // Si no es número, intentar parsear como nombre del enum (MuyBien, Bien, etc.)
            else if (Enum.TryParse<EstadoAnimoEnum>(mood, true, out estadoEnum))
            {
                // Éxito: se parseó como nombre del enum
            }
            else
            {
                return BadRequest(new { ok = false, error = $"Valor de mood inválido: {mood}" });
            }

            if (string.IsNullOrWhiteSpace(texto)) texto = null;

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = guid,
                EstadoMood = estadoEnum,
                Texto = texto,
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
                Id = nuevo.Id,
                EstadoMood = (int)nuevo.EstadoMood,
                EstadoMoodNombre = nuevo.EstadoMood.ToString(),
                Texto = nuevo.Texto,
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

        [HttpGet("estadisticas")]
        public async Task<ActionResult<object>> Estadisticas([FromQuery] int? meses = 1)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var fechaDesde = DateTime.UtcNow.AddMonths(-meses.Value);

            // ✅ CAMBIO: Cargar entidades primero, luego proyectar en memoria
            var entidades = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid && !x.Eliminado && x.FechaRegistro >= fechaDesde)
                .AsNoTracking()
                .ToListAsync();

            if (!entidades.Any())
                return Ok(new { total = 0, promedio = 0, maximo = 0, minimo = 0 });

            var registros = entidades.Select(x => (int)x.EstadoMood).ToList();

            return Ok(new
            {
                total = registros.Count,
                promedio = Math.Round(registros.Average(), 2),
                maximo = registros.Max(),
                minimo = registros.Min()
            });
        }
    }
}