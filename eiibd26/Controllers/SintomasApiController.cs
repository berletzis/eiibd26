using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    [Route("api/sintomas")]
    [ApiController]
    public class SintomasApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public SintomasApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new object[] { });

            var sintomas = await _db.sintomas
                .Where(s => !s.Eliminado && s.nombre.Contains(q))
                .OrderBy(s => s.nombre)
                .Select(s => new
                {
                    id = s.id,
                    nombre = s.nombre,
                    icono = s.icono, // Si tu modelo lo tiene
                })
                .ToListAsync();

            return Ok(sintomas);
        }
    }
}