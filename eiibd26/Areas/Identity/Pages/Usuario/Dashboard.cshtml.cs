using eiibd26.Data;
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
        private readonly IHealthStatsService _healthStats;
        private readonly IHealthInsightService _healthInsight;
        private readonly ITrackingSintomaService _trackingService;

        public DashboardModel(ApplicationDbContext db, IHealthStatsService healthStats, IHealthInsightService healthInsight, ITrackingSintomaService trackingService)
        {
            _db = db;
            _healthStats = healthStats;
            _healthInsight = healthInsight;
            _trackingService = trackingService;
        }

        // VM que la vista y el partial consumirán
        public DashboardViewModel VM { get; set; } = new DashboardViewModel();

        public async Task OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return;
            if (!Guid.TryParse(userIdClaim, out var userGuid)) return;

            // Período del dashboard: últimos 30 días (consistente con PDF "Último mes" y perfil público)
            var hasta = DateTime.Today;
            var desde = hasta.AddDays(-30);

            // ---------- Moods ----------
            var moods = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == userGuid && !x.Eliminado)
                .Where(x => x.FechaRegistro >= desde && x.FechaRegistro <= hasta)
                .OrderByDescending(x => x.FechaRegistro)
                .Select(x => new MoodPoint
                {
                    Fecha = x.FechaRegistro,
                    Estado = (int)x.EstadoMood,
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

            // Declared here so the analytics block below can access them without new queries
            var bySint = new Dictionary<int, List<TrackingSintomaUsuario>>();

            if (sintomasUsuarioList == null || !sintomasUsuarioList.Any())
            {
                VM.TopSintomas = new List<SymptomTopItem>();
            }
            else
            {
                var sintomaUsuarioIds = sintomasUsuarioList.Select(s => s.id).ToList();

                // Ventana: últimos 7 días (alineado con período del dashboard)
                var startDate = desde;
                var endDate   = hasta.AddDays(1).AddTicks(-1); // hasta fin del día

                // Trackings del usuario en ese rango
                var trackings = await _db.TrackingSintomaUsuario
                    .Where(t => t.IdUsuario == userGuid && sintomaUsuarioIds.Contains(t.IdSintomaUsuario) && t.Fecha >= startDate && t.Fecha <= endDate)
                    .Include(t => t.Frecuencia)
                    .ToListAsync();

                bySint = trackings
                    .GroupBy(t => t.IdSintomaUsuario)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var topItems = new List<SymptomTopItem>();
                foreach (var s in sintomasUsuarioList)
                {
                    var item = new SymptomTopItem
                    {
                        SintomaUsuarioId = s.id,
                        Nombre = s.Sintoma?.nombre ?? "(sin nombre)",
                        TipoSintoma = s.Sintoma?.TipoSintoma ?? 0,
                        Interacciones = bySint.ContainsKey(s.id) ? bySint[s.id].Count : 0,
                        Condiciones = new List<string>(),
                        SeguimientoPorDia = new Dictionary<string, DiaTracking>()
                    };

                    if (bySint.ContainsKey(s.id))
                    {
                        var tracksForSint = bySint[s.id];
                        var groupedByDay = tracksForSint
                            .GroupBy(x => x.Fecha.Date)
                            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Fecha).First());

                        foreach (var kv in groupedByDay)
                        {
                            var fechaKey = kv.Key.ToString("yyyy-MM-dd");
                            item.SeguimientoPorDia[fechaKey] = new DiaTracking
                            {
                                Estado        = kv.Value.Estado ?? "",
                                Dolor         = kv.Value.Dolor,
                                TieneSangrado = kv.Value.TieneSangrado,
                                FrecuenciaId  = kv.Value.FrecuenciaId
                            };
                        }

                        var dolorValues = tracksForSint.Where(t => t.Dolor.HasValue).Select(t => t.Dolor!.Value).ToList();
                        if (dolorValues.Count > 0)
                            item.PromedioDolor = Math.Round(dolorValues.Average(), 1);

                        var lastSangrado = tracksForSint
                            .Where(t => t.TieneSangrado.HasValue)
                            .OrderByDescending(t => t.Fecha)
                            .FirstOrDefault();
                        if (lastSangrado != null)
                            item.UltimoTieneSangrado = lastSangrado.TieneSangrado;

                        var lastFrecuencia = tracksForSint
                            .Where(t => t.FrecuenciaId.HasValue && t.Frecuencia != null)
                            .OrderByDescending(t => t.Fecha)
                            .FirstOrDefault();
                        if (lastFrecuencia != null)
                            item.UltimaFrecuenciaNombre = lastFrecuencia.Frecuencia!.Nombre;
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
            // CRÍTICO-01 + CRÍTICO-03: votos calculados inline con EntidadTipo canónico,
            // OrderBy y Take(2) ejecutados en BD, sin carga total en memoria.
            var preguntasVm = await _db.Preguntas
                .Where(p => p.UsuarioId == userGuid && !p.Eliminado)
                .Select(p => new QuestionItem
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    AnswersCount = p.Respuestas.Count(r => !r.Eliminado),
                    Votes = _db.Votos
                        .Where(v => v.EntidadTipo == eiibd26.Controllers.VotoTipo.Pregunta
                                    && v.EntidadId == p.Id
                                    && !v.Eliminado)
                        .Select(v => (int?)v.Valor).Sum() ?? 0,
                    FechaCreacion = p.FechaCreacion
                })
                .OrderByDescending(q =>
                    (double)(q.Votes * 2 + q.AnswersCount) /
                    (2.0 + EF.Functions.DateDiffDay(q.FechaCreacion, DateTime.UtcNow))
                )
                .Take(2)
                .ToListAsync();

            VM.Preguntas = preguntasVm;

            // ---------- Top Respuestas del usuario (top 2 por votos) ----------
            // CRÍTICO-01 + CRÍTICO-03: mismo patrón que preguntas.
            var respuestasVm = await _db.Respuestas
                .Where(r => r.UsuarioId == userGuid && !r.Eliminado)
                .Select(r => new AnswerItem
                {
                    Id = r.Id,
                    Cuerpo = r.Cuerpo,
                    PreguntaId = r.PreguntaId,
                    Votes = _db.Votos
                        .Where(v => v.EntidadTipo == eiibd26.Controllers.VotoTipo.Respuesta
                                    && v.EntidadId == r.Id
                                    && !v.Eliminado)
                        .Select(v => (int?)v.Valor).Sum() ?? 0
                })
                .OrderByDescending(a => a.Votes)
                .ThenByDescending(a => a.Id)
                .Take(2)
                .ToListAsync();

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
                VM.HasRealName = !string.IsNullOrWhiteSpace(perfil?.Nombre);
            }
            catch (Exception)
            {
                // No bloquear flujo por errores aquí
            }

            // ---------- Health Stats (sin nuevas queries) ----------
            var estadosExport = moods
                .Select(m => new EstadoAnimoExportDto
                {
                    FechaRegistro    = m.Fecha,
                    EstadoMood       = m.Estado,
                    EstadoMoodNombre = ((eiibd26.Models.EstadoAnimoEnum)m.Estado).ToString()
                })
                .ToList();

            // Agrupar trackings ya cargados por síntoma, mapeando a SintomaExportDto
            var sintomasExport = (sintomasUsuarioList ?? [])
                .Select(s => new SintomaExportDto
                {
                    NombreSintoma = s.Sintoma?.nombre ?? string.Empty,
                    Trackings = (bySint.TryGetValue(s.id, out var tList) ? tList : [])
                        .Select(t => new TrackingSintomaExportDto
                        {
                            Fecha            = t.Fecha,
                            Estado           = t.Estado ?? string.Empty,
                            Dolor            = t.Dolor,
                            TieneSangrado    = t.TieneSangrado,
                            FrecuenciaNombre = t.Frecuencia?.Nombre
                        })
                        .ToList()
                })
                .ToList();

            VM.HealthStats  = _healthStats.Calcular(estadosExport, sintomasExport);
            VM.HealthInsight = _healthInsight.Analizar(sintomasExport);

            VM.FrecuenciasCatalog = await _db.FrecuenciaSintomaCatalog
                .OrderBy(f => f.Orden)
                .AsNoTracking()
                .ToListAsync();
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

            int? dolor = null;
            if (form.ContainsKey("dolor") && int.TryParse(form["dolor"], out var dolorParsed))
                dolor = Math.Clamp(dolorParsed, 0, 10);

            int? frecuenciaId = null;
            if (form.ContainsKey("frecuenciaId") && int.TryParse(form["frecuenciaId"], out var frecParsed) && frecParsed > 0)
                frecuenciaId = frecParsed;

            bool? tieneSangrado = null;
            if (form.ContainsKey("tieneSangrado") && bool.TryParse(form["tieneSangrado"], out var sangradoParsed))
                tieneSangrado = sangradoParsed;

            var fechaStr = form["fecha"].ToString();
            if (!DateTime.TryParseExact(fechaStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                return BadRequest(new { success = false, error = "fecha inválida." });

            try
            {
                var resultado = await _trackingService.GuardarTrackingAsync(new TrackingRequestDto(
                    IdUsuario:        userGuid,
                    IdSintomaUsuario: sintomaUsuarioId,
                    Fecha:            fecha,
                    Estado:           estado,
                    Dolor:            dolor,
                    FrecuenciaId:     frecuenciaId,
                    TieneSangrado:    tieneSangrado
                ));
                return new JsonResult(new { success = true, estado = resultado.Estado, fecha = resultado.Fecha.ToString("yyyy-MM-dd") });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, error = "Error interno al guardar." });
            }
        }
    }
}