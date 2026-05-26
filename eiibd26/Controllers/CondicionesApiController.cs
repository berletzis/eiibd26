using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

        // SEC-003: Las condiciones EII (Crohn, Colitis, etc.) son taxonomía médica genérica pública.
        // El acceso anónimo es intencional. Rate limiting para prevenir scraping masivo.
        [HttpGet("autocomplete")]
        [AllowAnonymous]
        [EnableRateLimiting("catalogos-autocomplete")]
        public async Task<IActionResult> Autocomplete([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            // PERF-006: cargamos en una sola query todos los ids relevantes y sus padres,
            // luego resolvemos padreNombre y esPadre en memoria — elimina subqueries correlacionadas.
            var todas = await _db.condiciones
                .AsNoTracking()
                .Where(c => !c.Eliminado)
                .Select(c => new { c.id, c.nombre, c.idPadre, c.idIdioma, c.icono })
                .ToListAsync();

            // Índice para resolución de padreNombre en O(1)
            var porId = todas.ToDictionary(c => c.id);

            // Conjunto de ids que tienen hijos (para esPadre)
            var conHijos = todas
                .Where(c => c.idPadre.HasValue)
                .Select(c => c.idPadre!.Value)
                .ToHashSet();

            // Filtrar las que coinciden con q o cuyo padre coincide con q
            var condiciones = todas
                .Where(c =>
                    c.nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (c.idPadre.HasValue &&
                     porId.TryGetValue(c.idPadre.Value, out var padre) &&
                     padre.nombre.Contains(q, StringComparison.OrdinalIgnoreCase)))
                .Select(c => new
                {
                    id       = c.id,
                    nombre   = c.nombre,
                    idPadre  = c.idPadre,
                    padreNombre = c.idPadre.HasValue && porId.TryGetValue(c.idPadre.Value, out var p)
                                    ? p.nombre : null,
                    esPadre  = conHijos.Contains(c.id),
                    idIdioma = c.idIdioma,
                    icono    = c.icono
                })
                .ToList();

            // Construir resultado evitando duplicados y mostrando padres (si existen) seguido de sus hijos.
            var resultado = new List<object>();
            var agregado = new HashSet<int>();

            // Agregar padres de nivel superior primero (idPadre == null)
            var padresNivelSuperior = condiciones.Where(c => c.idPadre == null).OrderBy(c => c.nombre).ToList();
            foreach (var padre in padresNivelSuperior)
            {
                if (agregado.Add(padre.id))
                    resultado.Add(padre);

                foreach (var hijo in condiciones.Where(c => c.idPadre == padre.id).OrderBy(c => c.nombre))
                {
                    if (agregado.Add(hijo.id))
                        resultado.Add(hijo);
                }
            }

            // Agregar cualquier otro elemento restante, evitando duplicados
            foreach (var item in condiciones.OrderBy(c => c.padreNombre).ThenBy(c => c.nombre))
            {
                if (agregado.Add(item.id))
                    resultado.Add(item);
            }

            return Ok(resultado);
        }
    }
}