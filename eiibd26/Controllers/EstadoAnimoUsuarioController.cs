using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eiibd26.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class EstadoAnimoUsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ClinicalOwnershipValidator _ownership;

        public EstadoAnimoUsuarioController(ApplicationDbContext db, ClinicalOwnershipValidator ownership)
        {
            _db = db;
            _ownership = ownership;
        }

        [HttpGet("historico")]
        public async Task<ActionResult<List<object>>> Historico()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var registros = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid && !x.Eliminado)
                .OrderByDescending(x => x.FechaRegistro)
                .Take(200)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Include(x => x.SintomaUsuario).ThenInclude(su => su.Sintoma)
                .Include(x => x.TratamientoUsuario).ThenInclude(tu => tu.Tratamiento)
                .AsNoTracking()
                .ToListAsync();

            var lista = registros.Select(x => new
            {
                Id = x.Id,
                EstadoMood = (int)x.EstadoMood,
                EstadoMoodNombre = x.EstadoMood.ToString(),
                Texto = x.Texto,
                FechaRegistro = x.FechaRegistro.ToString("o"),
                Condicion = x.CondicionUsuario != null ? new { Id = x.CondicionUsuario.id, Nombre = x.CondicionUsuario.Condicion?.nombre } : null,
                Sintoma = x.SintomaUsuario != null ? new { Id = x.SintomaUsuario.id, Nombre = x.SintomaUsuario.Sintoma?.nombre } : null,
                Tratamiento = x.TratamientoUsuario != null ? new { Id = x.TratamientoUsuario.id, Nombre = x.TratamientoUsuario.Tratamiento?.nombre } : null
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
                .Select(x => new { id = x.id, idCondicion = x.idCondicion, nombre = x.Condicion.nombre })
                .ToListAsync();

            return Ok(condiciones);
        }

        [HttpGet("sintomas-usuario")]
        public async Task<ActionResult<List<object>>> SintomasUsuario()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var sintomas = await _db.sintomasUsuario
                .Where(x => x.idUsuario == guid && !x.Eliminado)
                .Include(x => x.Sintoma)
                .Select(x => new { id = x.id, idSintoma = x.idSintoma, nombre = x.Sintoma.nombre })
                .ToListAsync();

            return Ok(sintomas);
        }

        [HttpGet("tratamientos-usuario")]
        public async Task<ActionResult<List<object>>> TratamientosUsuario()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            var tratamientos = await _db.tratamientoUsuario
                .Where(x => x.idUsuario == guid && !x.Eliminado)
                .Include(x => x.Tratamiento)
                .Select(x => new { id = x.id, idTratamiento = x.idTratamiento, nombre = x.Tratamiento.nombre })
                .ToListAsync();

            return Ok(tratamientos);
        }

        [HttpPost("nuevo")]
        public async Task<ActionResult<object>> Nuevo(
            [FromForm] string mood,
            [FromForm] string? texto,
            [FromForm] int? condicionUsuarioId,
            [FromForm] int? sintomaUsuarioId,
            [FromForm] int? tratamientoUsuarioId,
            [FromForm] DateTime? fechaRegistro)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (!Guid.TryParse(userId, out var guid)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(mood))
                return BadRequest(new { ok = false, error = "El campo mood es requerido." });

            EstadoAnimoEnum estadoEnum;

            if (int.TryParse(mood, out int moodNumero))
            {
                if (moodNumero < 1 || moodNumero > 5)
                    return BadRequest(new { ok = false, error = "Valor de mood debe estar entre 1 y 5." });

                estadoEnum = (EstadoAnimoEnum)moodNumero;
            }
            else if (Enum.TryParse<EstadoAnimoEnum>(mood, true, out estadoEnum))
            {
                // Éxito
            }
            else
            {
                return BadRequest(new { ok = false, error = $"Valor de mood inválido: {mood}" });
            }

            if (string.IsNullOrWhiteSpace(texto)) texto = null;

            if (texto?.Length > 2000)
                return BadRequest(new { ok = false, error = "El texto no puede superar 2000 caracteres." });

            // SEC-010: Validar que los FK opcionales pertenecen al usuario autenticado.
            // Evita que un cliente manipule IDs de otro paciente.
            var invalidField = await _ownership.ValidateEstadoAnimoRelationsAsync(
                condicionUsuarioId, sintomaUsuarioId, tratamientoUsuarioId, guid);
            if (invalidField is not null)
                return BadRequest(new { ok = false, error = $"El campo {invalidField} no pertenece al usuario autenticado." });

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = guid,
                EstadoMood = estadoEnum,
                Texto = texto,
                FechaRegistro = (fechaRegistro.HasValue
                    && fechaRegistro.Value <= DateTime.UtcNow
                    && fechaRegistro.Value >= DateTime.UtcNow.AddHours(-24))
                    ? DateTime.SpecifyKind(fechaRegistro.Value, DateTimeKind.Utc)
                    : DateTime.UtcNow,
                IdCondicionUsuario = condicionUsuarioId,
                IdSintomaUsuario = sintomaUsuarioId,
                IdTratamientoUsuario = tratamientoUsuarioId
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

            object condicion = null;
            object sintoma = null;
            object tratamiento = null;

            if (condicionUsuarioId.HasValue)
            {
                var condNombre = await _db.condicionUsuario
                    .Include(c => c.Condicion)
                    .Where(c => c.id == condicionUsuarioId)
                    .Select(c => c.Condicion.nombre)
                    .FirstOrDefaultAsync();
                condicion = new { Id = condicionUsuarioId.Value, Nombre = condNombre };
            }

            if (sintomaUsuarioId.HasValue)
            {
                var sintNombre = await _db.sintomasUsuario
                    .Include(s => s.Sintoma)
                    .Where(s => s.id == sintomaUsuarioId)
                    .Select(s => s.Sintoma.nombre)
                    .FirstOrDefaultAsync();
                sintoma = new { Id = sintomaUsuarioId.Value, Nombre = sintNombre };
            }

            if (tratamientoUsuarioId.HasValue)
            {
                var tratNombre = await _db.tratamientoUsuario
                    .Include(t => t.Tratamiento)
                    .Where(t => t.id == tratamientoUsuarioId)
                    .Select(t => t.Tratamiento.nombre)
                    .FirstOrDefaultAsync();
                tratamiento = new { Id = tratamientoUsuarioId.Value, Nombre = tratNombre };
            }

            return Ok(new
            {
                Id = nuevo.Id,
                EstadoMood = (int)nuevo.EstadoMood,
                EstadoMoodNombre = nuevo.EstadoMood.ToString(),
                Texto = nuevo.Texto,
                FechaRegistro = nuevo.FechaRegistro.ToString("o"),
                Condicion = condicion,
                Sintoma = sintoma,
                Tratamiento = tratamiento
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

            var mesesSeguro = Math.Clamp(meses ?? 1, 1, 24);
            var fechaDesde = DateTime.UtcNow.AddMonths(-mesesSeguro);

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