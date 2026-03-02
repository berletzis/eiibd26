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

            // Construir resultado evitando duplicados y mostrando padres (si existen) seguido de sus hijos.
            var resultado = new List<object>();
            var agregado = new HashSet<int>();

            // Agregar padres de nivel superior primero (idPadre == null)
            var padresNivelSuperior = condiciones.Where(c => c.idPadre == null).OrderBy(c => c.nombre).ToList();
            foreach (var padre in padresNivelSuperior)
            {
                if (agregado.Add(padre.id))
                {
                    resultado.Add(padre);
                }

                var hijos = condiciones.Where(c => c.idPadre == padre.id).OrderBy(c => c.nombre).ToList();
                foreach (var hijo in hijos)
                {
                    if (agregado.Add(hijo.id))
                    {
                        resultado.Add(hijo);
                    }
                }
            }

            // Agregar cualquier otro elemento restante (padres con idPadre != null o hijos sueltos), evitando duplicados
            foreach (var item in condiciones.OrderBy(c => c.padreNombre).ThenBy(c => c.nombre))
            {
                if (agregado.Add(item.id))
                {
                    resultado.Add(item);
                }
            }

            return Ok(resultado);
        }
    }
}