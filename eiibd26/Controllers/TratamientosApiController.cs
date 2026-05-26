using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    [Route("api/tratamientos")]
    [ApiController]
    public class TratamientosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TratamientosApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        // SEC-002: Los tratamientos/medicamentos son taxonomía médica genérica de EII, no datos de pacientes.
        // El acceso anónimo es intencional. Rate limiting para prevenir scraping masivo.
        [HttpGet("autocomplete")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogos-autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new object[] { });

            var tratamientos = await _db.tratamientos
                .AsNoTracking()
                .Where(t => !t.Eliminado && t.nombre.Contains(q))
                .OrderBy(t => t.nombre)
                .Select(t => new
                {
                    id = t.id,
                    nombre = t.nombre,
                    icono = t.icono
                })
                .ToListAsync();

            return Ok(tratamientos);
        }
    }
}