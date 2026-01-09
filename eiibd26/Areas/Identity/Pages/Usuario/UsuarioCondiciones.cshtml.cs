using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [IgnoreAntiforgeryToken]
    public class UsuarioCondicionesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsuarioCondicionesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<PadreCondicionGroup> MisCondicionesAgrupadas { get; set; } = new();

        public class CondicionConDatos
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
            public int TratamientosCount { get; set; }
            public int SintomasCount { get; set; }
        }

        public class PadreCondicionGroup
        {
            public string PadreNombre { get; set; }
            public List<CondicionConDatos> Hijos { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var userIdGuid = Guid.Parse(userId);

            // Trae hijo+padre y datos de vínculo (solo las asignadas de usuario).
            // Importante: capturamos también el id de la fila en condicionUsuario (CondicionUsuarioId)
            // porque las tablas de relación referencian esa PK, y los contadores deben agruparse por ella.
            var condiciones = await (from cu in _db.condicionUsuario
                                     join c in _db.condiciones on cu.idCondicion equals c.id
                                     join padre in _db.condiciones on c.idPadre equals padre.id into padres
                                     from padreJoin in padres.DefaultIfEmpty()
                                     where cu.idUsuario == userIdGuid && !cu.Eliminado && c.idPadre != null
                                     select new
                                     {
                                         CondicionUsuarioId = cu.id,       // PK de condicionUsuario (usada por relaciones)
                                         HijoId = c.id,                    // id de la condicion hija (tabla condiciones)
                                         HijoNombre = c.nombre,
                                         PadreNombre = padreJoin != null ? padreJoin.nombre : "(Sin padre)",
                                         cu.fechaInicio
                                     }).ToListAsync();

            if (!condiciones.Any())
            {
                MisCondicionesAgrupadas = new List<PadreCondicionGroup>();
                return;
            }

            var condUsuarioIds = condiciones.Select(x => x.CondicionUsuarioId).ToList();

            // TratamientosCount: contamos TratamientoCondicionUsuario que pertenezcan al usuario,
            // cuyo tratamientoUsuario esté activo (no eliminado) y su IdCondicionUsuario corresponda
            // a una de las filas de condicionUsuario obtenidas.
            var tratamientosCounts = await (from tcr in _db.TratamientoCondicionUsuario
                                            join tu in _db.tratamientoUsuario on tcr.IdTratamientoUsuario equals tu.id
                                            where tcr.IdUsuario == userIdGuid
                                                  && tu.idUsuario == userIdGuid
                                                  && !tu.Eliminado
                                                  && tcr.IdCondicionUsuario != null
                                                  && condUsuarioIds.Contains(tcr.IdCondicionUsuario.Value)
                                            group tcr by tcr.IdCondicionUsuario.Value into g
                                            select new { IdCondicionUsuario = g.Key, Count = g.Count() })
                                            .ToListAsync();

            // SintomasCount: contamos SintomaCondicionUsuario que pertenezcan al usuario,
            // cuyo sintomasUsuario esté activo (no eliminado) y su IdCondicionUsuario corresponda
            // a una de las filas de condicionUsuario obtenidas.
            var sintomasCounts = await (from scr in _db.SintomaCondicionUsuario
                                        join su in _db.sintomasUsuario on scr.IdSintomaUsuario equals su.id
                                        where scr.IdUsuario == userIdGuid
                                              && su.idUsuario == userIdGuid
                                              && !su.Eliminado
                                              && condUsuarioIds.Contains(scr.IdCondicionUsuario)
                                        group scr by scr.IdCondicionUsuario into g
                                        select new { IdCondicionUsuario = g.Key, Count = g.Count() })
                                       .ToListAsync();

            var tratCountMap = tratamientosCounts.ToDictionary(x => x.IdCondicionUsuario, x => x.Count);
            var sintCountMap = sintomasCounts.ToDictionary(x => x.IdCondicionUsuario, x => x.Count);

            MisCondicionesAgrupadas = condiciones
                .GroupBy(x => x.PadreNombre)
                .Select(g => new PadreCondicionGroup
                {
                    PadreNombre = g.Key,
                    Hijos = g.Select(h => new CondicionConDatos
                    {
                        // Id here is the child condition id (tabla condiciones) as shown in the UI
                        Id = h.HijoId,
                        Nombre = h.HijoNombre,
                        FechaInicio = h.fechaInicio ?? DateTime.UtcNow,
                        // counts looked up by condicionUsuarioId
                        TratamientosCount = tratCountMap.TryGetValue(h.CondicionUsuarioId, out var tc) ? tc : 0,
                        SintomasCount = sintCountMap.TryGetValue(h.CondicionUsuarioId, out var sc) ? sc : 0
                    }).ToList()
                })
                .ToList();
        }

        // Agregar condición HIJO (no permite padres)
        public async Task<IActionResult> OnPostAgregarCondicionAsync(int condicionId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var condicion = await _db.condiciones.FirstOrDefaultAsync(x => x.id == condicionId);
            if (condicion == null || condicion.idPadre == null)
                return new JsonResult(new { ok = false, mensaje = "Debes seleccionar una condición hija." }) { StatusCode = 400 };

            var yaExiste = await _db.condicionUsuario
                .AnyAsync(x => x.idUsuario == Guid.Parse(userId) && x.idCondicion == condicionId && !x.Eliminado);
            if (yaExiste)
                return new JsonResult(new { ok = true, mensaje = "Ya agregada." });

            var nueva = new condicionUsuario
            {
                idUsuario = Guid.Parse(userId),
                idCondicion = condicionId,
                fechaInicio = DateTime.Now,
                Eliminado = false
            };

            _db.condicionUsuario.Add(nueva);
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int condId, DateTime nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.condicionUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.idCondicion == condId && !x.Eliminado);

            if (rel != null)
            {
                rel.fechaInicio = nuevaFechaInicio;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostEliminarCondicionAsync(int condId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.condicionUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.idCondicion == condId && !x.Eliminado);

            if (rel != null)
            {
                rel.Eliminado = true;
                rel.fechaEliminado = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}