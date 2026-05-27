using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize(Roles = "Paciente,Administrador")]
    public class UsuarioTratamientosModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsuarioTratamientosModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<TratamientoConDatos> MisTratamientos { get; set; } = new();
        public List<SintomaUsuarioSimple> MisSintomasSimplificados { get; set; } = new();
        public List<CondicionUsuarioSimple> MisCondicionesSimplificadas { get; set; } = new();

        public class TratamientoConDatos
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime? FechaFin { get; set; }
            public bool EsPrincipal { get; set; }
            public List<RelSimple> Sintomas { get; set; } = new();
            public List<RelSimple> Condiciones { get; set; } = new();
            public int SintomasCount => Sintomas?.Count ?? 0;
            public int CondicionesCount => Condiciones?.Count ?? 0;
        }
        public class RelSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }
        public class SintomaUsuarioSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }
        public class CondicionUsuarioSimple
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (User.IsInRole("Medico")) { Response.Redirect("/Identity/Medico/Dashboard"); return; }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var userIdGuid = Guid.Parse(userId);

            MisSintomasSimplificados = await (from su in _db.sintomasUsuario
                                              join s in _db.sintomas on su.idSintoma equals s.id
                                              where su.idUsuario == userIdGuid && !su.Eliminado
                                              select new SintomaUsuarioSimple
                                              {
                                                  Id = su.id,
                                                  Nombre = s.nombre
                                              }).ToListAsync();

            MisCondicionesSimplificadas = await (from cu in _db.condicionUsuario
                                                 join c in _db.condiciones on cu.idCondicion equals c.id
                                                 where cu.idUsuario == userIdGuid && !cu.Eliminado
                                                 select new CondicionUsuarioSimple
                                                 {
                                                     Id = cu.id,
                                                     Nombre = c.nombre
                                                 }).ToListAsync();

            // IMPORTANT: ensure related-symptom and related-condition subqueries are filtered by IdUsuario
            MisTratamientos = await (from tu in _db.tratamientoUsuario
                                     join t in _db.tratamientos on tu.idTratamiento equals t.id
                                     where tu.idUsuario == userIdGuid && !tu.Eliminado
                                     select new TratamientoConDatos
                                     {
                                         Id = tu.id,
                                         Nombre = t.nombre,
                                         EsPrincipal = tu.EsPrincipal,
                                         FechaInicio = tu.fechaInicio >= new DateTime(1753, 1, 1) ? tu.fechaInicio : tu.fechaCreado,
                                         FechaFin = tu.FechaFin,
                                         Sintomas = (
                                             from rel in _db.TratamientoSintomaUsuario
                                             join su in _db.sintomasUsuario on rel.IdSintomaUsuario equals su.id
                                             join s in _db.sintomas on su.idSintoma equals s.id
                                             where rel.IdTratamientoUsuario == tu.id
                                                   && rel.IdUsuario == tu.idUsuario   // ensure relation belongs to same user
                                                   && !su.Eliminado                    // only show active sintomasUsuario
                                             select new RelSimple
                                             {
                                                 Id = rel.IdSintomaUsuario ?? 0,
                                                 Nombre = s.nombre
                                             }).ToList(),
                                         Condiciones = (
                                             from rel in _db.TratamientoCondicionUsuario
                                             join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                                             join c in _db.condiciones on cu.idCondicion equals c.id
                                             where rel.IdTratamientoUsuario == tu.id
                                                   && rel.IdUsuario == tu.idUsuario   // ensure relation belongs to same user
                                                   && !cu.Eliminado                    // only show active condicionUsuario
                                             select new RelSimple
                                             {
                                                 Id = rel.IdCondicionUsuario ?? 0,
                                                 Nombre = c.nombre
                                             }).ToList()
                                     }).ToListAsync();
        }

        // ---- POST HANDLERS ----

        public async Task<IActionResult> OnPostAgregarTratamientoAsync(int tratamientoId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();

                var tratamiento = await _db.tratamientos.FirstOrDefaultAsync(x => x.id == tratamientoId);
                if (tratamiento == null)
                    return new JsonResult(new { ok = false, mensaje = "Tratamiento no encontrado" }) { StatusCode = 400 };

                bool existe = await _db.tratamientoUsuario.AnyAsync(x =>
                    x.idUsuario == Guid.Parse(userId) && x.idTratamiento == tratamientoId && !x.Eliminado);
                if (existe)
                    return new JsonResult(new { ok = true, mensaje = "Ya agregado" });

                var tu = new tratamientoUsuario
                {
                    idUsuario = Guid.Parse(userId),
                    idTratamiento = tratamientoId,
                    fechaInicio = DateTime.Now,
                    fechaCreado = DateTime.Now,
                    fechaModificado = DateTime.Now,
                    Eliminado = false
                };
                _db.tratamientoUsuario.Add(tu);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, mensaje = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int tratId, string? nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!DateTime.TryParseExact(nuevaFechaInicio, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var fechaParsed))
                return new JsonResult(new { ok = false, mensaje = "Fecha no válida." }) { StatusCode = 400 };

            var rel = await _db.tratamientoUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == tratId && !x.Eliminado);

            if (rel != null)
            {
                rel.fechaInicio = (fechaParsed < new DateTime(1753, 1, 1)) ? DateTime.Now : fechaParsed;
                rel.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostEditarFechaFinAsync(int tratId, string nuevaFechaFin = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            DateTime? fechaFin = null;
            if (!string.IsNullOrWhiteSpace(nuevaFechaFin) && DateTime.TryParse(nuevaFechaFin, out var parsed))
                fechaFin = parsed;

            var rel = await _db.tratamientoUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == tratId && !x.Eliminado);

            if (rel != null)
            {
                if (fechaFin.HasValue && fechaFin.Value < rel.fechaInicio)
                    return new JsonResult(new { ok = false, mensaje = "La fecha de fin no puede ser anterior a la fecha de inicio." }) { StatusCode = 400 };

                rel.FechaFin = fechaFin;
                rel.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostEliminarTratamientoAsync(int tratId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null) return Unauthorized();
                var userGuid = Guid.Parse(userId);

                var tratamiento = await _db.tratamientoUsuario
                    .FirstOrDefaultAsync(x => x.idUsuario == userGuid && x.id == tratId && !x.Eliminado);

                if (tratamiento == null)
                    return new JsonResult(new { ok = false, mensaje = "No encontrado o ya eliminado." }) { StatusCode = 404 };

                // Check relations filtered by the current user AND ensure the referenced child is not soft-deleted
                var tieneSintomas = await (
                    from rel in _db.TratamientoSintomaUsuario
                    join su in _db.sintomasUsuario on rel.IdSintomaUsuario equals su.id
                    where rel.IdUsuario == userGuid && rel.IdTratamientoUsuario == tratId && !su.Eliminado
                    select rel
                ).AnyAsync();

                var tieneCondiciones = await (
                    from rel in _db.TratamientoCondicionUsuario
                    join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                    where rel.IdUsuario == userGuid && rel.IdTratamientoUsuario == tratId && !cu.Eliminado
                    select rel
                ).AnyAsync();

                if (tieneSintomas || tieneCondiciones)
                    return new JsonResult(new { ok = false, mensaje = "Elimina primero los s�ntomas o condiciones asociados." }) { StatusCode = 400 };

                tratamiento.Eliminado = true;
                tratamiento.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();

                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { ok = false, mensaje = ex.Message });
            }
        }

        // ---- ASOCIAR S�NTOMAS/CODICIONES ----

        public async Task<IActionResult> OnPostAsociarSintomasAsync(int tratamientoId, List<int> sintomaUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var relsActuales = await _db.TratamientoSintomaUsuario
                .Where(x => x.IdUsuario == Guid.Parse(userId) && x.IdTratamientoUsuario == tratamientoId)
                .ToListAsync();

            var idsValidos = await _db.sintomasUsuario
                .Where(x => sintomaUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
                .Select(x => x.id)
                .ToListAsync();

            _db.TratamientoSintomaUsuario.RemoveRange(relsActuales.Where(x => !idsValidos.Contains(x.IdSintomaUsuario ?? 0)));

            foreach (var sId in idsValidos)
            {
                if (!relsActuales.Any(x => x.IdSintomaUsuario == sId))
                {
                    _db.TratamientoSintomaUsuario.Add(new TratamientoSintomaUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdTratamientoUsuario = tratamientoId,
                        IdSintomaUsuario = sId,
                        FechaCreado = DateTime.Now,
                        Notas = null
                    });
                }
            }
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostAsociarCondicionesAsync(int tratamientoId, List<int> condicionUsuarioIds)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var relsActuales = await _db.TratamientoCondicionUsuario
                .Where(x => x.IdUsuario == Guid.Parse(userId) && x.IdTratamientoUsuario == tratamientoId)
                .ToListAsync();

            var idsValidos = await _db.condicionUsuario
                .Where(x => condicionUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
                .Select(x => x.id)
                .ToListAsync();

            _db.TratamientoCondicionUsuario.RemoveRange(relsActuales.Where(x => !idsValidos.Contains(x.IdCondicionUsuario ?? 0)));

            foreach (var cId in idsValidos)
            {
                if (!relsActuales.Any(x => x.IdCondicionUsuario == cId))
                {
                    _db.TratamientoCondicionUsuario.Add(new TratamientoCondicionUsuario
                    {
                        IdUsuario = Guid.Parse(userId),
                        IdTratamientoUsuario = tratamientoId,
                        IdCondicionUsuario = cId,
                        FechaCreado = DateTime.Now,
                        Notas = null
                    });
                }
            }
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostQuitarRelacionSintomaAsync(int tratamientoId, int sintomaUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.TratamientoSintomaUsuario
                .FirstOrDefaultAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdTratamientoUsuario == tratamientoId && x.IdSintomaUsuario == sintomaUsuarioId);
            if (rel != null)
            {
                _db.TratamientoSintomaUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false });
        }

        public async Task<IActionResult> OnPostQuitarRelacionCondicionAsync(int tratamientoId, int condicionUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.TratamientoCondicionUsuario
                .FirstOrDefaultAsync(x => x.IdUsuario == Guid.Parse(userId) && x.IdTratamientoUsuario == tratamientoId && x.IdCondicionUsuario == condicionUsuarioId);
            if (rel != null)
            {
                _db.TratamientoCondicionUsuario.Remove(rel);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return new JsonResult(new { ok = false });
        }

        /// <summary>Marca/desmarca un tratamiento del usuario como principal (solo uno puede estar activo).</summary>
        public async Task<IActionResult> OnPostTogglePrincipalTratamientoAsync(int tratId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var userGuid = Guid.Parse(userId);

            var objetivo = await _db.tratamientoUsuario
                .FirstOrDefaultAsync(x => x.id == tratId && x.idUsuario == userGuid && !x.Eliminado);
            if (objetivo == null) return NotFound();

            var nuevoValor = !objetivo.EsPrincipal;

            if (nuevoValor)
            {
                var otros = await _db.tratamientoUsuario
                    .Where(x => x.idUsuario == userGuid && !x.Eliminado && x.EsPrincipal && x.id != tratId)
                    .ToListAsync();
                foreach (var o in otros) o.EsPrincipal = false;
            }

            objetivo.EsPrincipal = nuevoValor;
            objetivo.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true, esPrincipal = nuevoValor });
        }
    }
}