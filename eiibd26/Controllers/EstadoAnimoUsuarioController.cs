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
            var guid = Guid.Parse(userId);

            var lista = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == guid)
                .OrderByDescending(x => x.FechaRegistro)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Select(x => new
                {
                    EstadoMood = x.EstadoMood,
                    Texto = x.Texto,
                    FechaRegistro = x.FechaRegistro,
                    RelacionNombre = x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre : null,
                    TipoRelacion = x.CondicionUsuario != null ? "Condicion" : null
                })
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("condiciones-usuario")]
        public async Task<ActionResult<List<object>>> CondicionesUsuario()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var guid = Guid.Parse(userId);

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
            var guid = Guid.Parse(userId);

            if (string.IsNullOrWhiteSpace(texto)) texto = null;

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = guid,
                EstadoMood = mood,
                Texto = texto,
                FechaRegistro = DateTime.Now,
                IdCondicionUsuario = condicionUsuarioId
            };
            _db.EstadoAnimoUsuario.Add(nuevo);
            await _db.SaveChangesAsync();

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
                FechaRegistro = nuevo.FechaRegistro,
                RelacionNombre = nombre,
                TipoRelacion = tipo
            });
        }
    }
}