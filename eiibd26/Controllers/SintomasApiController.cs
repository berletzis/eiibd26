using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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

        // SEC-001: Los síntomas son taxonomía médica genérica de EII, no datos de pacientes.
        // El acceso anónimo es intencional para soportar autocomplete en UI pública futura.
        // Rate limiting aplicado para prevenir scraping masivo.
        [HttpGet("autocomplete")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogos-autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] string excludeIds = null)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new object[] { });

            // Parse comma-separated excluded sintoma catalog IDs
            var excluded = new HashSet<int>();
            if (!string.IsNullOrWhiteSpace(excludeIds))
            {
                foreach (var part in excludeIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                    if (int.TryParse(part.Trim(), out var eid))
                        excluded.Add(eid);
            }

            var query = _db.sintomas
                .AsNoTracking()
                .Where(s => !s.Eliminado && s.nombre.Contains(q));

            if (excluded.Count > 0)
                query = query.Where(s => !excluded.Contains(s.id));

            var sintomas = await query
                .OrderBy(s => s.nombre)
                .Select(s => new
                {
                    id = s.id,
                    nombre = s.nombre,
                    icono = s.icono,
                })
                .ToListAsync();

            return Ok(sintomas);
        }
    }
}