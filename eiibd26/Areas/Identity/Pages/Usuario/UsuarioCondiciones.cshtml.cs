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

            // Trae hijo+padre y datos de vínculo (solo las asignadas de usuario)
            var condiciones = await (from cu in _db.condicionUsuario
                                     join c in _db.condiciones on cu.idCondicion equals c.id
                                     join padre in _db.condiciones on c.idPadre equals padre.id into padres
                                     from padreJoin in padres.DefaultIfEmpty()
                                     where cu.idUsuario == userIdGuid && !cu.Eliminado && c.idPadre != null
                                     select new
                                     {
                                         HijoId = c.id,
                                         HijoNombre = c.nombre,
                                         PadreNombre = padreJoin != null ? padreJoin.nombre : "(Sin padre)",
                                         cu.fechaInicio,
                                         // Tratamientos asociados a la condición del usuario
                                         TratamientosCount = _db.TratamientoCondicionUsuario
                                            .Count(tcu => tcu.IdCondicionUsuario == cu.id && tcu.IdUsuario == userIdGuid),
                                         // Síntomas asociados a la condición del usuario
                                         SintomasCount = _db.SintomaCondicionUsuario
                                            .Count(scu => scu.IdCondicionUsuario == cu.id && scu.IdUsuario == userIdGuid)
                                     }
                                    ).ToListAsync();

            MisCondicionesAgrupadas = condiciones
                .GroupBy(x => x.PadreNombre)
                .Select(g => new PadreCondicionGroup
                {
                    PadreNombre = g.Key,
                    Hijos = g.Select(h => new CondicionConDatos
                    {
                        Id = h.HijoId,
                        Nombre = h.HijoNombre,
                        FechaInicio = h.fechaInicio ?? DateTime.UtcNow,
                        TratamientosCount = h.TratamientosCount,
                        SintomasCount = h.SintomasCount
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