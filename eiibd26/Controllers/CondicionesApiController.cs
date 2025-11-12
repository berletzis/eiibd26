using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Controllers
{
    [Route("api/condiciones")]
    [ApiController]
    public class CondicionesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CondicionesApiController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            // Obtener condiciones no eliminadas que coinciden con el nombre o el nombre del padre
            var condiciones = await _db.condiciones
                .Where(c => !c.Eliminado && (
                        c.nombre.Contains(q) ||
                        (c.idPadre != null && _db.condiciones.Any(p => p.id == c.idPadre && p.nombre.Contains(q)))
                    ))
                .Select(c => new
                {
                    id = c.id,
                    nombre = c.nombre,
                    idPadre = c.idPadre,
                    padreNombre = c.idPadre != null
                        ? _db.condiciones.Where(p => p.id == c.idPadre).Select(p => p.nombre).FirstOrDefault()
                        : null,
                    esPadre = _db.condiciones.Any(x => x.idPadre == c.id && !x.Eliminado),
                    idIdioma = c.idIdioma,
                    icono = c.icono
                })
                .OrderBy(c => c.padreNombre).ThenBy(c => c.nombre)
                .ToListAsync();

            // Encuentra IDs de padres en la respuesta
            var padresIds = condiciones.Where(c => c.esPadre).Select(c => c.id).Distinct().ToList();
            var padres = condiciones.Where(c => c.idPadre == null && c.esPadre).ToList();

            // Armar resultado: padres primero, luego hijos
            var resultado = new List<object>();
            foreach (var padre in padres)
            {
                resultado.Add(padre);
                var hijos = condiciones.Where(c => c.idPadre == padre.id).ToList();
                resultado.AddRange(hijos);
            }
            // Agrega hijos sin padre (caso aislado)
            var hijosSinPadre = condiciones.Where(c => c.idPadre != null && !padresIds.Contains(c.idPadre.Value)).ToList();
            resultado.AddRange(hijosSinPadre);

            return Ok(resultado);
        }
    }
}