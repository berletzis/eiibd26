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
    public class UsuarioSintomasModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsuarioSintomasModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<SintomaConDatos> MisSintomas { get; set; } = new();
        public List<CondicionUsuarioSimple> MisCondicionesSimplificadas { get; set; } = new();
        public List<TratamientoUsuarioSimple> MisTratamientosSimplificados { get; set; } = new();

        public class SintomaConDatos
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
            public List<RelSimple> Condiciones { get; set; } = new();
            public List<RelSimple> Tratamientos { get; set; } = new();
            public int CondicionesCount => Condiciones?.Count ?? 0;
            public int TratamientosCount => Tratamientos?.Count ?? 0;
        }

        public class RelSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public class CondicionUsuarioSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public class TratamientoUsuarioSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var userIdGuid = Guid.Parse(userId);

            MisCondicionesSimplificadas = await (from cu in _db.condicionUsuario
                                                 join c in _db.condiciones on cu.idCondicion equals c.id
                                                 where cu.idUsuario == userIdGuid && !cu.Eliminado
                                                 select new CondicionUsuarioSimple
                                                 {
                                                     Id = cu.id,
                                                     Nombre = c.nombre
                                                 }).ToListAsync();

            MisTratamientosSimplificados = await (from tu in _db.tratamientoUsuario
                                                  join t in _db.tratamientos on tu.idTratamiento equals t.id
                                                  where tu.idUsuario == userIdGuid && !tu.Eliminado
                                                  select new TratamientoUsuarioSimple
                                                  {
                                                      Id = tu.id,
                                                      Nombre = t.nombre
                                                  }).ToListAsync();

            MisSintomas = await (from su in _db.sintomasUsuario
                                 join s in _db.sintomas on su.idSintoma equals s.id
                                 where su.idUsuario == userIdGuid && !su.Eliminado
                                 select new SintomaConDatos
                                 {
                                     Id = su.id,
                                     Nombre = s.nombre,
                                     FechaInicio = su.fechaInicio >= new DateTime(1753, 1, 1) ? su.fechaInicio : su.fechaCreado,
                                     Condiciones = (from rel in _db.SintomaCondicionUsuario
                                                    join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                                                    join c in _db.condiciones on cu.idCondicion equals c.id
                                                    where rel.IdSintomaUsuario == su.id && !cu.Eliminado
                                                    select new RelSimple
                                                    {
                                                        Id = rel.IdCondicionUsuario,
                                                        Nombre = c.nombre
                                                    }).ToList(),
                                     Tratamientos = (from rel in _db.TratamientoSintomaUsuario
                                                     join tu in _db.tratamientoUsuario on rel.IdTratamientoUsuario equals tu.id
                                                     join t in _db.tratamientos on tu.idTratamiento equals t.id
                                                     where rel.IdSintomaUsuario == su.id && !tu.Eliminado
                                                     select new RelSimple
                                                     {
                                                         Id = rel.IdTratamientoUsuario ?? 0,
                                                         Nombre = t.nombre
                                                     }).ToList()
                                 }).ToListAsync();
        }

        public async Task<IActionResult> OnPostAgregarSintomaAsync(int sintomaId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var sintoma = await _db.sintomas.FirstOrDefaultAsync(x => x.id == sintomaId);
                if (sintoma == null)
                    return new JsonResult(new { ok = false, mensaje = "Síntoma no encontrado" }) { StatusCode = 400 };

                bool existe = await _db.sintomasUsuario.AnyAsync(x =>
                    x.idUsuario == Guid.Parse(userId) && x.idSintoma == sintomaId && !x.Eliminado);
                if (existe)
                    return new JsonResult(new { ok = true, mensaje = "Ya agregado" });

                var su = new sintomasUsuario
                {
                    idUsuario = Guid.Parse(userId),
                    idSintoma = sintomaId,
                    fechaInicio = DateTime.Now,
                    fechaCreado = DateTime.Now,
                    fechaModificado = DateTime.Now,
                    Eliminado = false
                };
                _db.sintomasUsuario.Add(su);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, mensaje = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int sintId, DateTime nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == sintId && !x.Eliminado);

            if (rel != null)
            {
                rel.fechaInicio = (nuevaFechaInicio < new DateTime(1753, 1, 1)) ? DateTime.Now : nuevaFechaInicio;
                rel.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostEliminarSintomaAsync(int sintId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var sintoma = await _db.sintomasUsuario
                    .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == sintId && !x.Eliminado);

                if (sintoma == null)
                    return new JsonResult(new { ok = false, mensaje = "No encontrado o ya eliminado." }) { StatusCode = 404 };

                // No permitir eliminar si tiene condiciones o tratamientos asociados
                var tieneCondiciones = await _db.SintomaCondicionUsuario
                    .AnyAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintId);
                var tieneTratamientos = await _db.TratamientoSintomaUsuario
                    .AnyAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintId);
                if (tieneCondiciones || tieneTratamientos)
                    return new JsonResult(new { ok = false, mensaje = "Elimina primero las condiciones y tratamientos asociados." }) { StatusCode = 400 };

                sintoma.Eliminado = true;
                sintoma.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();

                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, mensaje = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostAsociarCondicionesAsync(int sintomaId, List<int> condicionUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var relsActuales = await _db.SintomaCondicionUsuario
                .Where(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintomaId)
                .ToListAsync();

            _db.SintomaCondicionUsuario.RemoveRange(relsActuales.Where(x => !condicionUsuarioIds.Contains(x.IdCondicionUsuario)));

            foreach (var condId in condicionUsuarioIds)
            {
                if (!relsActuales.Any(x => x.IdCondicionUsuario == condId))
                {
                    _db.SintomaCondicionUsuario.Add(new SintomaCondicionUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdCondicionUsuario = condId,
                        IdSintomaUsuario = sintomaId,
                        FechaCreado = DateTime.Now,
                        Notas = null
                    });
                }
            }
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostAsociarTratamientosAsync(int sintomaId, List<int> tratamientoUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var relsActuales = await _db.TratamientoSintomaUsuario
                .Where(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintomaId)
                .ToListAsync();

            _db.TratamientoSintomaUsuario.RemoveRange(relsActuales.Where(x => !tratamientoUsuarioIds.Contains(x.IdTratamientoUsuario ?? 0)));

            foreach (var tId in tratamientoUsuarioIds)
            {
                if (!relsActuales.Any(x => x.IdTratamientoUsuario == tId))
                {
                    _db.TratamientoSintomaUsuario.Add(new TratamientoSintomaUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdSintomaUsuario = sintomaId,
                        IdTratamientoUsuario = tId,
                        FechaCreado = DateTime.Now,
                        Notas = null
                    });
                }
            }
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostQuitarRelacionCondicionAsync(int sintomaId, int condicionUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.SintomaCondicionUsuario
                .FirstOrDefaultAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintomaId && x.IdCondicionUsuario == condicionUsuarioId);
            if (rel != null)
            {
                _db.SintomaCondicionUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false });
        }

        public async Task<IActionResult> OnPostQuitarRelacionTratamientoAsync(int sintomaId, int tratamientoUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.TratamientoSintomaUsuario
                .FirstOrDefaultAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdSintomaUsuario == sintomaId && x.IdTratamientoUsuario == tratamientoUsuarioId);
            if (rel != null)
            {
                _db.TratamientoSintomaUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false });
        }
    }
}