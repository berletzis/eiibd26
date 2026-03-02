using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DashboardModel(ApplicationDbContext db) => _db = db;

        // VM que la vista y el partial consumirán
        public DashboardViewModel VM { get; set; } = new DashboardViewModel();

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return;
            if (!Guid.TryParse(userIdClaim, out var userGuid)) return;

            // ---------- Moods ----------
            var moods = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == userGuid)
                .OrderByDescending(x => x.FechaRegistro)
                .Take(50)
                .Select(x => new MoodPoint
                {
                    Fecha = x.FechaRegistro,
                    Estado = (int)x.EstadoMood,  // ✅ Cast explícito
                    Texto = x.Texto,
                    RelacionNombre = x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre :
                                    (x.SintomaUsuario != null ? x.SintomaUsuario.Sintoma.nombre : null)
                })
                .ToListAsync();

            // ---------- Relaciones (opciones) ----------
            var relaciones = await _db.condicionUsuario
                .Where(c => c.idUsuario == userGuid && !c.Eliminado)
                .Include(c => c.Condicion)
                .Select(c => new RelationItem { Id = c.id, Nombre = c.Condicion.nombre, Tipo = "condicion" })
                .ToListAsync();

            var sintomasUsuarioRelations = await _db.sintomasUsuario
                .Where(s => s.idUsuario == userGuid && !s.Eliminado)
                .Include(s => s.Sintoma)
                .Select(s => new RelationItem { Id = s.id, Nombre = s.Sintoma.nombre, Tipo = "sintoma" })
                .ToListAsync();

            relaciones.AddRange(sintomasUsuarioRelations);

            var tratamientosUsuario = await _db.tratamientoUsuario
                .Where(t => t.idUsuario == userGuid && !t.Eliminado)
                .Include(t => t.Tratamiento)
                .Select(t => new RelationItem { Id = t.id, Nombre = t.Tratamiento.nombre, Tipo = "tratamiento" })
                .ToListAsync();

            relaciones.AddRange(tratamientosUsuario);

            VM.Moods = moods;
            VM.MoodRelations = relaciones;

            // ---------- TopSintomas + SeguimientoPorDia + Condiciones ----------
            var sintomasUsuarioList = await _db.sintomasUsuario
                .Where(s => s.idUsuario == userGuid && !s.Eliminado)
                .Include(s => s.Sintoma)
                .ToListAsync();

            if (sintomasUsuarioList == null || !sintomasUsuarioList.Any())
            {
                VM.TopSintomas = new List<SymptomTopItem>();
            }
            else
            {
                var sintomaUsuarioIds = sintomasUsuarioList.Select(s => s.id).ToList();

                // Ventana: ayer + hoy
                var startDate = DateTime.Today.AddDays(-1).Date; // inicio de ayer 00:00
                var endDate = DateTime.Today.AddDays(1).Date.AddTicks(-1); // fin de hoy

                // Trackings del usuario en ese rango
                var trackings = await _db.TrackingSintomaUsuario
                    .Where(t => t.IdUsuario == userGuid && sintomaUsuarioIds.Contains(t.IdSintomaUsuario) && t.Fecha >= startDate && t.Fecha <= endDate)
                    .ToListAsync();

                var bySint = trackings
                    .GroupBy(t => t.IdSintomaUsuario)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var topItems = new List<SymptomTopItem>();
                foreach (var s in sintomasUsuarioList)
                {
                    var item = new SymptomTopItem
                    {
                        SintomaUsuarioId = s.id,
                        Nombre = s.Sintoma?.nombre ?? "(sin nombre)",
                        Interacciones = bySint.ContainsKey(s.id) ? bySint[s.id].Count : 0,
                        Condiciones = new List<string>(),
                        SeguimientoPorDia = new Dictionary<string, string>()
                    };

                    if (bySint.ContainsKey(s.id))
                    {
                        var groupedByDay = bySint[s.id]
                            .GroupBy(x => x.Fecha.Date)
                            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Fecha).First());

                        foreach (var kv in groupedByDay)
                        {
                            var fechaKey = kv.Key.ToString("yyyy-MM-dd");
                            item.SeguimientoPorDia[fechaKey] = kv.Value.Estado ?? "";
                        }
                    }

                    topItems.Add(item);
                }

                // Rellenar Condiciones relacionadas (usando navegación)
                var scList = await _db.SintomaCondicionUsuario
                    .Where(sc => sc.IdUsuario == userGuid && sintomaUsuarioIds.Contains(sc.IdSintomaUsuario))
                    .Include(sc => sc.CondicionUsuario)
                        .ThenInclude(cu => cu.Condicion)
                    .ToListAsync();

                if (scList.Any())
                {
                    var condMapBySint = scList
                        .GroupBy(sc => sc.IdSintomaUsuario)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(sc => sc.CondicionUsuario?.Condicion?.nombre)
                                  .Where(n => !string.IsNullOrWhiteSpace(n))
                                  .Distinct()
                                  .ToList()
                        );

                    foreach (var itm in topItems)
                    {
                        if (condMapBySint.TryGetValue(itm.SintomaUsuarioId, out var conds))
                        {
                            itm.Condiciones = conds;
                        }
                    }
                }

                VM.TopSintomas = topItems
                    .OrderByDescending(x => x.Interacciones)
                    .ThenBy(x => x.Nombre)
                    .Take(5)
                    .ToList();
            }

            // ---------- Top Preguntas del usuario (top 2 por votos) ----------
            var preguntasUsuario = await _db.Preguntas
                .Where(p => p.UsuarioId == userGuid && !p.Eliminado)
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    AnswersCount = p.Respuestas.Count(r => !r.Eliminado)
                })
                .ToListAsync();

            var preguntaIds = preguntasUsuario.Select(p => p.Id).ToList();
            Dictionary<Guid, int> votosPreguntas = new Dictionary<Guid, int>();
            if (preguntaIds.Any())
            {
                votosPreguntas = await _db.Votos
                    .Where(v => v.EntidadTipo == "Pregunta" && preguntaIds.Contains(v.EntidadId))
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Id, x => x.Count);
            }

            var preguntasVm = preguntasUsuario
                .Select(p => new QuestionItem
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    AnswersCount = p.AnswersCount,
                    Votes = votosPreguntas.TryGetValue(p.Id, out var cnt) ? cnt : 0
                })
                .OrderByDescending(q => q.Votes)
                .ThenByDescending(q => q.AnswersCount)
                .Take(2)
                .ToList();

            VM.Preguntas = preguntasVm;

            // ---------- Top Respuestas del usuario (top 2 por votos) ----------
            var respuestasUsuario = await _db.Respuestas
                .Where(r => r.UsuarioId == userGuid && !r.Eliminado)
                .Select(r => new
                {
                    r.Id,
                    r.Cuerpo,
                    r.PreguntaId
                })
                .ToListAsync();

            var respuestaIds = respuestasUsuario.Select(r => r.Id).ToList();
            Dictionary<Guid, int> votosRespuestas = new Dictionary<Guid, int>();
            if (respuestaIds.Any())
            {
                votosRespuestas = await _db.Votos
                    .Where(v => v.EntidadTipo == "Respuesta" && respuestaIds.Contains(v.EntidadId))
                    .GroupBy(v => v.EntidadId)
                    .Select(g => new { Id = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Id, x => x.Count);
            }

            var respuestasVm = respuestasUsuario
                .Select(r => new AnswerItem
                {
                    Id = r.Id,
                    Cuerpo = r.Cuerpo,
                    Votes = votosRespuestas.TryGetValue(r.Id, out var cnt2) ? cnt2 : 0,
                    PreguntaId = r.PreguntaId
                })
                .OrderByDescending(a => a.Votes)
                .ThenByDescending(a => a.Id)
                .Take(2)
                .ToList();

            VM.Respuestas = respuestasVm;

            // ---------- Notifications / quick checks ----------
            var appUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
            VM.EmailConfirmed = appUser?.EmailConfirmed ?? false;
            VM.PhoneNumberConfirmed = appUser?.PhoneNumberConfirmed ?? false;

            VM.HasAnyCondition = await _db.condicionUsuario.AnyAsync(c => c.idUsuario == userGuid && !c.Eliminado);

            var todayStart = DateTime.Today;
            var todayEnd = DateTime.Today.AddDays(1);
            VM.HasMoodToday = await _db.EstadoAnimoUsuario.AnyAsync(m => m.IdUsuario == userGuid && m.FechaRegistro >= todayStart && m.FechaRegistro < todayEnd);

            var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);
            var newAnswersCount = await _db.Respuestas
                .Where(r => r.FechaCreacion >= weekAgo && r.UsuarioId != userGuid)
                .Join(_db.Preguntas, r => r.PreguntaId, p => p.Id, (r, p) => new { r, p })
                .Where(x => x.p.UsuarioId == userGuid)
                .CountAsync();
            VM.NewAnswersCount = newAnswersCount;

            VM.ScheduledItemsCount = 0;

            // ---- Diagnosis date check: si alguna condicion del usuario tiene fechaInicio igual
            // a la fecha de creación del perfil (o a FechaCreado) avisar que actualice la fecha.
            try
            {
                var perfil = await _db.Perfil.AsNoTracking().FirstOrDefaultAsync(p => p.idUser == userGuid);
                DateTime? perfilCreado = perfil?.FechaCreado ?? perfil?.FechaCreacion ?? null;
                if (perfilCreado.HasValue)
                {
                    var diagMatches = await _db.condicionUsuario
                        .Where(cu => cu.idUsuario == userGuid && !cu.Eliminado && cu.fechaInicio != null && cu.fechaCreado != null)
                        .AsNoTracking()
                        .ToListAsync();

                    var countMatches = diagMatches.Count(cu => cu.fechaInicio.Value.Date == perfilCreado.Value.Date);
                    VM.DiagnosisUpdatesCount = countMatches;
                    VM.NeedsDiagnosisDateUpdate = countMatches > 0;
                }
            }
            catch (Exception)
            {
                // No bloquear flujo por errores aquí
            }
        }

        public async Task<IActionResult> OnPostTrackSintomaMatriz()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return Challenge();
            if (!Guid.TryParse(userIdClaim, out var userGuid)) return Challenge();

            var form = Request.Form;
            if (!form.ContainsKey("sintomaUsuarioId") || !form.ContainsKey("estado") || !form.ContainsKey("fecha"))
                return BadRequest(new { success = false, error = "Faltan parámetros." });

            if (!int.TryParse(form["sintomaUsuarioId"], out var sintomaUsuarioId))
                return BadRequest(new { success = false, error = "sintomaUsuarioId inválido." });

            var estado = form["estado"].ToString();

            var fechaStr = form["fecha"].ToString();
            if (!DateTime.TryParseExact(fechaStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return BadRequest(new { success = false, error = "fecha inválida." });

            var dayStart = fecha.Date;
            var dayEnd = fecha.Date.AddDays(1).AddTicks(-1);

            try
            {
                var now = DateTime.Now;
                var timestampForRecord = fecha.Date.Add(now.TimeOfDay);

                var existing = await _db.TrackingSintomaUsuario
                    .Where(t => t.IdUsuario == userGuid && t.IdSintomaUsuario == sintomaUsuarioId && t.Fecha >= dayStart && t.Fecha <= dayEnd)
                    .FirstOrDefaultAsync();

                if (existing != null)
                {
                    existing.Estado = estado;
                    existing.Fecha = timestampForRecord;
                    _db.TrackingSintomaUsuario.Update(existing);
                    await _db.SaveChangesAsync();
                    return new JsonResult(new { success = true, estado = existing.Estado, fecha = existing.Fecha.ToString("yyyy-MM-dd") });
                }
                else
                {
                    var nuevo = new TrackingSintomaUsuario
                    {
                        IdUsuario = userGuid,
                        IdSintomaUsuario = sintomaUsuarioId,
                        Fecha = timestampForRecord,
                        Estado = estado
                    };
                    _db.TrackingSintomaUsuario.Add(nuevo);
                    await _db.SaveChangesAsync();
                    return new JsonResult(new { success = true, estado = nuevo.Estado, fecha = nuevo.Fecha.ToString("yyyy-MM-dd") });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Error interno al guardar." });
            }
        }
    }
}