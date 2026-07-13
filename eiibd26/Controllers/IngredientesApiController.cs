using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Route("api/ingredientes")]
    [ApiController]
    public class IngredientesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public IngredientesApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Espejo de CondicionesApiController: catálogo genérico público, acceso anónimo
        // intencional + rate limiting para prevenir scraping. Alimenta el .eii-autocomplete
        // de ingredientes (captura admin y perfil del paciente).
        [HttpGet("autocomplete")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogos-autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            var term = q.Trim();

            // Modelo plano (sin jerarquía): filtramos en DB. Solo activos (§7.2: los
            // desactivados no se ofrecen para registros nuevos). Coincidencia por nombre
            // del ingrediente o por su grupo, análogo al "nombre o padre" de condiciones.
            // Take(20): los ingredientes crecen sin tope, a diferencia de la taxonomía de condiciones.
            var resultado = await _db.PlatIngredientes
                .AsNoTracking()
                .Where(i => i.Activo &&
                            (i.Nombre.Contains(term) ||
                             (i.Grupo != null && i.Grupo.Nombre.Contains(term))))
                .OrderBy(i => i.Nombre)
                .Take(20)
                .Select(i => new
                {
                    id = i.Id,
                    nombre = i.Nombre,
                    grupo = i.Grupo != null ? i.Grupo.Nombre : null
                })
                .ToListAsync();

            return Ok(resultado);
        }
    }
}
