using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;
using eiibd26.DTOs.Tracking;
using eiibd26.Models;
using eiibd26.Services.Analytics;
using eiibd26.Services.Tracking;
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
    public class UsuarioSintomasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ITrackingSintomaService _trackingService;
        private readonly IHealthStatsService _healthStats;

        public UsuarioSintomasModel(ApplicationDbContext db, ITrackingSintomaService trackingService, IHealthStatsService healthStats)
        {
            _db = db;
            _trackingService = trackingService;
            _healthStats = healthStats;
        }

        /// <summary>Tendencia por s�ntoma, clave = NombreSintoma.Trim(). Sin datos = "Sin datos suficientes".</summary>
        public Dictionary<string, SymptomTrendDto> TendenciasPorSintoma { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public List<SintomaConDatos> MisSintomas { get; set; } = new();
        public List<CondicionUsuarioSimple> MisCondicionesSimplificadas { get; set; } = new();
        public List<TratamientoUsuarioSimple> MisTratamientosSimplificados { get; set; } = new();
        public List<FrecuenciaSintomaCatalog> FrecuenciasCatalog { get; set; } = new();

        public class SintomaConDatos
        {
            public int Id { get; set; }
            /// <summary>ID del cat�logo de s�ntomas (sintomas.id), usado para excluir en autocomplete.</summary>
            public int CatalogoSintomaId { get; set; }
            public string Nombre { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime? FechaFin { get; set; }
            public bool EsPrincipal { get; set; }
            public int TipoSintoma { get; set; } = 0;
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
            if (User.IsInRole("Medico")) { Response.Redirect("/Identity/Medico/Dashboard"); return; }
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
                                             su.idSintoma,
                                             su.fechaInicio,
                                             su.FechaFin,
                                             su.fechaCreado,
                                             su.EsPrincipal,
                                             s.nombre,
                                             s.TipoSintoma
                                         }).ToListAsync();

            var desde = DateTime.Today.AddDays(-60);
            var trackings = await _db.TrackingSintomaUsuario
                .Where(t => t.IdUsuario == userIdGuid && t.Fecha >= desde)
                .ToListAsync();

            // Relaciones condiciones
            var relacionesCondiciones = await (
                from rel in _db.SintomaCondicionUsuario
                join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                join c in _db.condiciones on cu.idCondicion equals c.id
                where cu.idUsuario == userIdGuid
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
                where tu.idUsuario == userIdGuid
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
                    CatalogoSintomaId = su.idSintoma ?? 0,
                    Nombre = su.nombre,
                    EsPrincipal = su.EsPrincipal,
                    TipoSintoma = su.TipoSintoma,
                    FechaInicio = su.fechaInicio >= new DateTime(1753, 1, 1) ? su.fechaInicio : su.fechaCreado,
                    FechaFin = su.FechaFin,
                    Condiciones = condiciones,
                    Tratamientos = tratamientos,
                    UltimoTracking = ultimo
                };
            }).ToList();

            FrecuenciasCatalog = await _db.FrecuenciaSintomaCatalog
                .OrderBy(f => f.Orden)
                .ToListAsync();

            // Calcular tendencias usando trackings ya en memoria (60 d�as) � sin nueva query
            var sintomasExport = MapearSintomasExport(
                sintomasUsuario.Select(su => (su.id, su.nombre)).ToList(), trackings);
            var stats = _healthStats.Calcular([], sintomasExport);
            TendenciasPorSintoma = stats.Symptoms.PorSintoma
                .ToDictionary(t => t.NombreSintoma.Trim(), t => t, StringComparer.OrdinalIgnoreCase);
        }

        private static List<SintomaExportDto> MapearSintomasExport(
            List<(int id, string nombre)> sintomas,
            List<TrackingSintomaUsuario> trackings)
        {
            return sintomas.Select(su => new SintomaExportDto
            {
                NombreSintoma = su.nombre,
                Tratamientos  = [],
                Trackings     = trackings
                    .Where(t => t.IdSintomaUsuario == su.id)
                    .OrderBy(t => t.Fecha)
                    .Select(t => new TrackingSintomaExportDto
                    {
                        Fecha  = t.Fecha,
                        Estado = t.Estado ?? string.Empty,
                        Dolor  = t.Dolor
                    }).ToList()
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
                    return new JsonResult(new { ok = false, mensaje = "S�ntoma no encontrado" }) { StatusCode = 400 };

                bool existe = await _db.sintomasUsuario.AnyAsync(x =>
                    x.idUsuario == Guid.Parse(userId) && x.idSintoma == sintomaId && !x.Eliminado);
                if (existe)
                    return new JsonResult(new { ok = true, mensaje = "Ya agregado" });

                var su = new sintomasUsuario
                {
                    idUsuario = Guid.Parse(userId),
                    idSintoma = sintomaId,
                    fechaInicio = DateTime.UtcNow,
                    fechaCreado = DateTime.UtcNow,
                    fechaModificado = DateTime.UtcNow,
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

        public async Task<IActionResult> OnPostTrackSintomaAsync(int sintomaUsuarioId, string? estado, int? dolor, bool? tieneSangrado, int? frecuenciaId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(estado)) return BadRequest();

            var existeSintoma = await _db.sintomasUsuario
                .Include(su => su.Sintoma)
                .AnyAsync(x => x.id == sintomaUsuarioId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);
            if (!existeSintoma) return BadRequest();

            var userGuid = Guid.Parse(userId);
            await _trackingService.GuardarTrackingAsync(new TrackingRequestDto(
                IdUsuario:        userGuid,
                IdSintomaUsuario: sintomaUsuarioId,
                Fecha:            DateTime.Today,
                Estado:           estado,
                Dolor:            dolor.HasValue ? Math.Clamp(dolor.Value, 0, 10) : null,
                FrecuenciaId:     frecuenciaId > 0 ? frecuenciaId : null,
                TieneSangrado:    tieneSangrado
            ));
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

            // FUNC-007: Filtrar solo IDs que pertenecen al usuario autenticado
            var idsValidos = await _db.condicionUsuario
                .Where(x => condicionUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
                .Select(x => x.id)
                .ToListAsync();

            // Agrega nuevas relaciones que no exist�an
            foreach (var condId in idsValidos)
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

            // FUNC-008: Filtrar solo IDs que pertenecen al usuario autenticado
            var idsValidos = await _db.tratamientoUsuario
                .Where(x => tratamientoUsuarioIds.Contains(x.id) && x.idUsuario == Guid.Parse(userId) && !x.Eliminado)
                .Select(x => x.id)
                .ToListAsync();

            // Agrega nuevas relaciones
            foreach (var tratId in idsValidos)
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
                return new JsonResult(new { ok = false, mensaje = "No se encontr� el s�ntoma, o ya fue eliminado." }) { StatusCode = 400 };

            bool tieneCondiciones = await _db.SintomaCondicionUsuario
                .AnyAsync(x => x.IdSintomaUsuario == sintId);
            if (tieneCondiciones)
                return new JsonResult(new { ok = false, mensaje = "No se puede eliminar el s�ntoma porque tiene condiciones relacionadas. Primero qu�talas." }) { StatusCode = 400 };

            bool tieneTratamientos = await _db.TratamientoSintomaUsuario
                .AnyAsync(x => x.IdSintomaUsuario == sintId);
            if (tieneTratamientos)
                return new JsonResult(new { ok = false, mensaje = "No se puede eliminar el s�ntoma porque tiene tratamientos relacionados. Primero qu�talos." }) { StatusCode = 400 };

            var tieneTracking = await _db.TrackingSintomaUsuario
                .AnyAsync(t => t.IdSintomaUsuario == sintId);
            if (tieneTracking)
                return new JsonResult(new { ok = false, mensaje = "No se puede eliminar el s�ntoma porque tiene registros de seguimiento. Contacta con soporte si necesitas eliminarlo." }) { StatusCode = 400 };

            sintomaUsuario.Eliminado = true;
            sintomaUsuario.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true });
        }

        public async Task<IActionResult> OnPostEditarFechaInicioAsync(int sintId, string? nuevaFechaInicio)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            if (!DateTime.TryParseExact(nuevaFechaInicio, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var fechaParsed))
                return new JsonResult(new { ok = false, mensaje = "Fecha no válida." }) { StatusCode = 400 };

            var sintomaUsuario = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.id == sintId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);

            if (sintomaUsuario == null)
                return new JsonResult(new { ok = false, mensaje = "Sintoma no encontrado" }) { StatusCode = 400 };

            var fechaMin = new DateTime(1900, 1, 1);
            if (fechaParsed < fechaMin || fechaParsed > DateTime.Today)
                return new JsonResult(new { ok = false, mensaje = "La fecha debe estar entre 1900 y hoy." }) { StatusCode = 400 };

            sintomaUsuario.fechaInicio = fechaParsed;
            sintomaUsuario.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { ok = true });
        }

        /// <summary>Fija o limpia la fecha de fin (opcional) de un sintoma del usuario.</summary>
        public async Task<IActionResult> OnPostEditarFechaFinAsync(int sintId, string? nuevaFechaFin = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            DateTime? fechaFin = null;
            if (!string.IsNullOrWhiteSpace(nuevaFechaFin) && DateTime.TryParse(nuevaFechaFin, out var parsed))
                fechaFin = parsed;

            var rel = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.idUsuario == Guid.Parse(userId) && x.id == sintId && !x.Eliminado);

            if (rel != null)
            {
                // fechaInicio es DateTime no-nullable y puede traer el sentinel 1753: solo validar si es una fecha real.
                if (fechaFin.HasValue && rel.fechaInicio >= new DateTime(1753, 1, 1) && fechaFin.Value < rel.fechaInicio)
                    return new JsonResult(new { ok = false, mensaje = "La fecha de fin no puede ser anterior a la fecha de inicio." }) { StatusCode = 400 };

                rel.FechaFin = fechaFin;
                rel.fechaModificado = DateTime.Now;
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            return BadRequest();
        }

        /// <summary>Marca/desmarca un s�ntoma del usuario como principal (solo uno puede estar activo).</summary>
        public async Task<IActionResult> OnPostTogglePrincipalSintomaAsync(int sintId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            var userGuid = Guid.Parse(userId);

            var objetivo = await _db.sintomasUsuario
                .FirstOrDefaultAsync(x => x.id == sintId && x.idUsuario == userGuid && !x.Eliminado);
            if (objetivo == null) return NotFound();

            var nuevoValor = !objetivo.EsPrincipal;

            if (nuevoValor)
            {
                var otros = await _db.sintomasUsuario
                    .Where(x => x.idUsuario == userGuid && !x.Eliminado && x.EsPrincipal && x.id != sintId)
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