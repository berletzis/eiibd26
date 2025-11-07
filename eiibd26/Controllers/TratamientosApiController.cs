using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new object[] { });

            var tratamientos = await _db.tratamientos
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