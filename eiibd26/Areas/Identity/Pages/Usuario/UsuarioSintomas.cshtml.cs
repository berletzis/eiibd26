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
            public TrackingSintomaUsuario UltimoTracking { get; set; }
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

            var sintomasUsuario = await (from su in _db.sintomasUsuario
                                         join s in _db.sintomas on su.idSintoma equals s.id
                                         where su.idUsuario == userIdGuid && !su.Eliminado
                                         select new
                                         {
                                             su.id,
                                             su.fechaInicio,
                                             su.fechaCreado,
                                             s.nombre
                                         }).ToListAsync();

            var trackings = await _db.TrackingSintomaUsuario
                .Where(t => t.IdUsuario == userIdGuid)
                .ToListAsync();

            // Relaciones condiciones
            var relacionesCondiciones = await (
                from rel in _db.SintomaCondicionUsuario
                join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                join c in _db.condiciones on cu.idCondicion equals c.id
                select new
                {
                    rel.IdSintomaUsuario,
                    Id = rel.IdCondicionUsuario,
                    Nombre = c.nombre,
                    Eliminado = cu.Eliminado
                }
            ).ToListAsync();

            // Relaciones tratamientos
            var relacionesTratamientos = await (
                from rel in _db.TratamientoSintomaUsuario
                join tu in _db.tratamientoUsuario on rel.IdTratamientoUsuario equals tu.id
                join t in _db.tratamientos on tu.idTratamiento equals t.id
                select new
                {
                    rel.IdSintomaUsuario,
                    Id = rel.IdTratamientoUsuario ?? 0,
                    Nombre = t.nombre,
                    Eliminado = tu.Eliminado
                }
            ).ToListAsync();

            MisSintomas = sintomasUsuario.Select(su => {
                var condiciones = relacionesCondiciones
                                  .Where(rc => rc.IdSintomaUsuario == su.id && !rc.Eliminado)
                                  .Select(rc => new RelSimple { Id = rc.Id, Nombre = rc.Nombre })
                                  .ToList();

                var tratamientos = relacionesTratamientos
                                   .Where(rt => rt.IdSintomaUsuario == su.id && !rt.Eliminado)
                                   .Select(rt => new RelSimple { Id = rt.Id, Nombre = rt.Nombre })
                                   .ToList();

                var ultimo = trackings.Where(t => t.IdSintomaUsuario == su.id)
                                      .OrderByDescending(t => t.Fecha)
                                      .FirstOrDefault();

                return new SintomaConDatos
                {
                    Id = su.id,
                    Nombre = su.nombre,
                    FechaInicio = su.fechaInicio >= new DateTime(1753, 1, 1) ? su.fechaInicio : su.fechaCreado,
                    Condiciones = condiciones,
                    Tratamientos = tratamientos,
                    UltimoTracking = ultimo
                };
            }).ToList();
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

        public async Task<IActionResult> OnPostTrackSintomaAsync(int sintomaUsuarioId, string estado)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(estado)) return BadRequest();

            var existeSintoma = await _db.sintomasUsuario.AnyAsync(x => x.id == sintomaUsuarioId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);
            if (!existeSintoma) return BadRequest();

            var tracking = new TrackingSintomaUsuario
            {
                IdUsuario = Guid.Parse(userId),
                IdSintomaUsuario = sintomaUsuarioId,
                Fecha = DateTime.Now,
                Estado = estado
            };
            _db.TrackingSintomaUsuario.Add(tracking);
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        // ... (dentro de tu clase UsuarioSintomasModel)

        public async Task<IActionResult> OnPostAsociarCondicionesAsync(int sintomaId, List<int> condicionUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var existentes = await _db.SintomaCondicionUsuario
                .Where(x => x.IdSintomaUsuario == sintomaId && x.IdUsuario == Guid.Parse(userId))
                .ToListAsync();

            // Borra relaciones no seleccionadas
            _db.SintomaCondicionUsuario.RemoveRange(
                existentes.Where(x => !condicionUsuarioIds.Contains(x.IdCondicionUsuario))
            );

            // Agrega nuevas relaciones que no existían
            foreach (var condId in condicionUsuarioIds)
            {
                if (!existentes.Any(x => x.IdCondicionUsuario == condId))
                {
                    _db.SintomaCondicionUsuario.Add(new SintomaCondicionUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdSintomaUsuario = sintomaId,
                        IdCondicionUsuario = condId,
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
                .FirstOrDefaultAsync(x => x.IdSintomaUsuario == sintomaId && x.IdCondicionUsuario == condicionUsuarioId && x.IdUsuario == Guid.Parse(userId));
            if (rel != null)
            {
                _db.SintomaCondicionUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false, mensaje = "No encontrada" });
        }

        public async Task<IActionResult> OnPostAsociarTratamientosAsync(int sintomaId, List<int> tratamientoUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var existentes = await _db.TratamientoSintomaUsuario
                .Where(x => x.IdSintomaUsuario == sintomaId && x.IdUsuario == Guid.Parse(userId))
                .ToListAsync();

            // Borra relaciones no seleccionadas
            _db.TratamientoSintomaUsuario.RemoveRange(
                existentes.Where(x => !tratamientoUsuarioIds.Contains(x.IdTratamientoUsuario ?? 0))
            );

            // Agrega nuevas relaciones
            foreach (var tratId in tratamientoUsuarioIds)
            {
                if (!existentes.Any(x => (x.IdTratamientoUsuario ?? 0) == tratId))
                {
                    _db.TratamientoSintomaUsuario.Add(new TratamientoSintomaUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdSintomaUsuario = sintomaId,
                        IdTratamientoUsuario = tratId,
                        FechaCreado = DateTime.Now,
                        Notas = null
                    });
                }
            }
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostQuitarRelacionTratamientoAsync(int sintomaId, int tratamientoUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.TratamientoSintomaUsuario
                .FirstOrDefaultAsync(x => x.IdSintomaUsuario == sintomaId && x.IdTratamientoUsuario == tratamientoUsuarioId && x.IdUsuario == Guid.Parse(userId));
            if (rel != null)
            {
                _db.TratamientoSintomaUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false, mensaje = "No encontrada" });
        }

        public async Task<IActionResult> OnPostEliminarSintomaAsync(int sintId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var sintomaUsuario = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.id == sintId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);

            if (sintomaUsuario == null)
                return new JsonResult(new { ok = false, mensaje = "No se encontró el síntoma, o ya fue eliminado." }) { StatusCode = 400 };

            sintomaUsuario.Eliminado = true;
            sintomaUsuario.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int sintId, DateTime nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var sintomaUsuario = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.id == sintId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);

            if (sintomaUsuario == null)
                return new JsonResult(new { ok = false, mensaje = "Síntoma no encontrado" }) { StatusCode = 400 };

            sintomaUsuario.fechaInicio = nuevaFechaInicio;
            sintomaUsuario.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true });
        }
    }
}