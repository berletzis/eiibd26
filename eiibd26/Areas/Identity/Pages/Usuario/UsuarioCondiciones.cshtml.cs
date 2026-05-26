using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize(Roles = "Paciente,Administrador")]
    public class UsuarioCondicionesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioCondicionesModel> _logger;

        public UsuarioCondicionesModel(ApplicationDbContext db, ILogger<UsuarioCondicionesModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public List<PadreCondicionGroup> MisCondicionesAgrupadas { get; set; } = new();

        public class CondicionConDatos
        {
            public int Id { get; set; }
            public int CondicionUsuarioId { get; set; }
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
            public int TratamientosCount { get; set; }
            public int SintomasCount { get; set; }
            public bool FechaCoincideConRegistro { get; set; }
            public bool EsPrincipal { get; set; }
        }

        public class PadreCondicionGroup
        {
            public string PadreNombre { get; set; }
            public List<CondicionConDatos> Hijos { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            if (User.IsInRole("Medico")) { Response.Redirect("/Identity/Medico/Dashboard"); return; }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var userIdGuid = Guid.Parse(userId);

            // Trae hijo+padre y datos de v�nculo (solo las asignadas de usuario).
            // Importante: capturamos tambi�n el id de la fila en condicionUsuario (CondicionUsuarioId)
            // porque las tablas de relaci�n referencian esa PK, y los contadores deben agruparse por ella.
            // Trae tanto condiciones hijas como condiciones padre (si el usuario guard� la relaci�n
            // apuntando a la condici�n padre). Para las condiciones que no tienen padre, usamos
            // el propio nombre como "PadreNombre" para que se muestren en la UI.
            var condiciones = await (from cu in _db.condicionUsuario
                                     join c in _db.condiciones on cu.idCondicion equals c.id
                                     join padre in _db.condiciones on c.idPadre equals padre.id into padres
                                     from padreJoin in padres.DefaultIfEmpty()
                                     where cu.idUsuario == userIdGuid && !cu.Eliminado
                                     select new
                                     {
                                         CondicionUsuarioId = cu.id,
                                         cu.EsPrincipal,
                                         HijoId = c.id,
                                         HijoNombre = c.nombre,
                                         PadreNombre = padreJoin != null ? padreJoin.nombre : c.nombre,
                                         cu.fechaInicio
                                     }).ToListAsync();

            if (!condiciones.Any())
            {
                MisCondicionesAgrupadas = new List<PadreCondicionGroup>();
                return;
            }

            var condUsuarioIds = condiciones.Select(x => x.CondicionUsuarioId).ToList();

            // Obtener fecha de creaci�n del perfil para comparar
            var perfil = await _db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == userIdGuid);
            DateTime? perfilCreado = perfil?.FechaCreado ?? perfil?.FechaCreacion;

            // TratamientosCount: contamos TratamientoCondicionUsuario que pertenezcan al usuario,
            // cuyo tratamientoUsuario est� activo (no eliminado) y su IdCondicionUsuario corresponda
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
            // cuyo sintomasUsuario est� activo (no eliminado) y su IdCondicionUsuario corresponda
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
                        Id = h.HijoId,
                        CondicionUsuarioId = h.CondicionUsuarioId,
                        Nombre = h.HijoNombre,
                        EsPrincipal = h.EsPrincipal,
                        FechaInicio = h.fechaInicio ?? DateTime.UtcNow,
                        FechaCoincideConRegistro = perfilCreado.HasValue && h.fechaInicio.HasValue && h.fechaInicio.Value.Date == perfilCreado.Value.Date,
                        TratamientosCount = tratCountMap.TryGetValue(h.CondicionUsuarioId, out var tc) ? tc : 0,
                        SintomasCount = sintCountMap.TryGetValue(h.CondicionUsuarioId, out var sc) ? sc : 0
                    }).ToList()
                })
                .ToList();
        }

        // Agregar condici�n HIJO (no permite padres)
        public async Task<IActionResult> OnPostAgregarCondicionAsync(int condicionId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var condicion = await _db.condiciones.FirstOrDefaultAsync(x => x.id == condicionId && !x.Eliminado);
            if (condicion == null)
                return new JsonResult(new { ok = false, mensaje = "Condición no encontrada." }) { StatusCode = 400 };

            var tieneHijos = await _db.condiciones.AnyAsync(x => x.idPadre == condicionId && !x.Eliminado);
            if (tieneHijos)
                return new JsonResult(new { ok = false, mensaje = "Debes seleccionar una condición específica, no una categoría." }) { StatusCode = 400 };

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

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int condUsuarioId, DateTime? nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!nuevaFechaInicio.HasValue)
                return new JsonResult(new { ok = false, mensaje = "Fecha no válida." }) { StatusCode = 400 };

            var rel = await _db.condicionUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == condUsuarioId && !x.Eliminado);

            if (rel != null)
            {
                rel.fechaInicio = nuevaFechaInicio.Value;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        public async Task<IActionResult> OnPostEliminarCondicionAsync(int condUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var rel = await _db.condicionUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == condUsuarioId && !x.Eliminado);

            if (rel != null)
            {
                var tieneSintomas = await _db.SintomaCondicionUsuario
                    .AnyAsync(x => x.IdCondicionUsuario == rel.id);
                if (tieneSintomas)
                    return new JsonResult(new { ok = false, mensaje = "No se puede eliminar la condición porque tiene síntomas relacionados. Primero quítalos." }) { StatusCode = 400 };

                var tieneTratamientos = await _db.TratamientoCondicionUsuario
                    .AnyAsync(x => x.IdCondicionUsuario == rel.id);
                if (tieneTratamientos)
                    return new JsonResult(new { ok = false, mensaje = "No se puede eliminar la condición porque tiene tratamientos relacionados. Primero quítalos." }) { StatusCode = 400 };

                rel.Eliminado = true;
                rel.fechaEliminado = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        /// <summary>Marca/desmarca una condici�n del usuario como principal (solo una puede estar activa).</summary>
        public async Task<IActionResult> OnPostTogglePrincipalCondicionAsync(int condUsuarioId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var userGuid = Guid.Parse(userId);

            var objetivo = await _db.condicionUsuario
                .FirstOrDefaultAsync(x => x.id == condUsuarioId && x.idUsuario == userGuid && !x.Eliminado);
            if (objetivo == null) return NotFound();

            var nuevoValor = !objetivo.EsPrincipal;

            if (nuevoValor)
            {
                var otros = await _db.condicionUsuario
                    .Where(x => x.idUsuario == userGuid && !x.Eliminado && x.EsPrincipal && x.id != condUsuarioId)
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