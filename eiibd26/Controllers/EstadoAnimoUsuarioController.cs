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
                .Take(30)
                .Include(x => x.SintomaUsuario).ThenInclude(s => s.Sintoma)
                .Include(x => x.CondicionUsuario).ThenInclude(c => c.Condicion)
                .Include(x => x.TratamientoUsuario).ThenInclude(t => t.Tratamiento)
.Select(x => new {
    EstadoMood = x.EstadoMood,           // <<-- con MAYÚSCULAS!
    Texto = x.Texto,
    FechaRegistro = x.FechaRegistro,
    RelacionNombre = x.SintomaUsuario != null ? x.SintomaUsuario.Sintoma.nombre :
                    x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre :
                    x.TratamientoUsuario != null ? x.TratamientoUsuario.Tratamiento.nombre : null,
    TipoRelacion = x.SintomaUsuario != null ? "Sintoma" :
                   x.CondicionUsuario != null ? "Condicion" :
                   x.TratamientoUsuario != null ? "Tratamiento" : null
})
                .ToListAsync();
            return Ok(lista);
        }

        [HttpPost("nuevo")]
        public async Task<ActionResult<object>> Nuevo([FromForm] string mood, [FromForm] string texto, [FromForm] int? sintomaUsuarioId, [FromForm] int? condicionUsuarioId, [FromForm] int? tratamientoUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var guid = Guid.Parse(userId);

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = guid,
                EstadoMood = mood,
                Texto = texto,
                FechaRegistro = DateTime.Now,
                IdSintomaUsuario = sintomaUsuarioId,
                IdCondicionUsuario = condicionUsuarioId,
                IdTratamientoUsuario = tratamientoUsuarioId
            };
            _db.EstadoAnimoUsuario.Add(nuevo);
            await _db.SaveChangesAsync();

            string nombre = null; string tipo = null;
            if (sintomaUsuarioId.HasValue)
            { nombre = await _db.sintomasUsuario.Include(s => s.Sintoma).Where(s => s.id == sintomaUsuarioId).Select(s => s.Sintoma.nombre).FirstOrDefaultAsync(); tipo = "Sintoma"; }
            else if (condicionUsuarioId.HasValue)
            { nombre = await _db.condicionUsuario.Include(c => c.Condicion).Where(c => c.id == condicionUsuarioId).Select(c => c.Condicion.nombre).FirstOrDefaultAsync(); tipo = "Condicion"; }
            else if (tratamientoUsuarioId.HasValue)
            { nombre = await _db.tratamientoUsuario.Include(t => t.Tratamiento).Where(t => t.id == tratamientoUsuarioId).Select(t => t.Tratamiento.nombre).FirstOrDefaultAsync(); tipo = "Tratamiento"; }

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