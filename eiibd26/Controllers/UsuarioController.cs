using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public UsuarioController(ApplicationDbContext db) { _db = db; }

        [HttpGet("RelacionesMoodOptions")]
        public async Task<IActionResult> GetRelacionesMoodOptions()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var guid = Guid.Parse(userId);

            var sintomas = await _db.sintomasUsuario
                .Where(s => s.idUsuario == guid && !s.Eliminado)
                .Include(s => s.Sintoma)
                .Select(s => new { id = s.id, nombre = s.Sintoma.nombre })
                .ToListAsync();

            var condiciones = await _db.condicionUsuario
                .Where(c => c.idUsuario == guid && !c.Eliminado)
                .Include(c => c.Condicion)
                .Select(c => new { id = c.id, nombre = c.Condicion.nombre })
                .ToListAsync();

            var tratamientos = await _db.tratamientoUsuario
                .Where(t => t.idUsuario == guid && !t.Eliminado)
                .Include(t => t.Tratamiento)
                .Select(t => new { id = t.id, nombre = t.Tratamiento.nombre })
                .ToListAsync();

            return Ok(new { sintomas, condiciones, tratamientos });
        }
    }
}