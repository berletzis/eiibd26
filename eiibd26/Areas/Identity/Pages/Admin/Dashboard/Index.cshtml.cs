using eiibd26.Data;
using eiibd26.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Dashboard
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly IAdminDashboardService _dashboardService;
        private readonly ApplicationDbContext _db;
        private static readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public IndexModel(IAdminDashboardService dashboardService, ApplicationDbContext db)
        {
            _dashboardService = dashboardService;
            _db = db;
        }

        public AdminDashboardDto Dto { get; set; } = new();

        public async Task OnGetAsync()
        {
            Dto = await _dashboardService.GetDashboardAsync();
        }

        // ── Chart: nuevos usuarios por semana (últimas 12 semanas) ──
        public async Task<IActionResult> OnGetChartUsuariosAsync()
        {
            var desde = DateTime.UtcNow.AddDays(-83); // ~12 semanas
            var rows = await _db.Perfil.AsNoTracking()
                .Where(p => p.FechaCreacion >= desde)
                .Select(p => p.FechaCreacion)
                .ToListAsync();

            var grouped = rows
                .GroupBy(d => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                .OrderBy(g => g.Key)
                .Select(g => new { semana = "Sem " + g.Key, total = g.Count() })
                .ToList();

            return new JsonResult(new { ok = true, data = grouped }, _json);
        }

        // ── Chart: preguntas + respuestas humanas por semana (últimas 12 semanas) ──
        public async Task<IActionResult> OnGetChartComunidadAsync()
        {
            var desde = DateTime.UtcNow.AddDays(-83);
            var preguntas = await _db.Preguntas.AsNoTracking()
                .Where(p => !p.Eliminado && p.FechaCreacion >= desde)
                .Select(p => p.FechaCreacion.UtcDateTime)
                .ToListAsync();
            var respuestas = await _db.Respuestas.AsNoTracking()
                .Where(r => !r.EsIA && !r.Eliminado && r.FechaCreacion >= desde)
                .Select(r => r.FechaCreacion.UtcDateTime)
                .ToListAsync();

            var allDates = preguntas.Concat(respuestas).Distinct()
                .GroupBy(d => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                .OrderBy(g => g.Key)
                .Select(g => g.Key)
                .ToList();

            var labels = allDates.Select(w => "Sem " + w).ToList();
            var pregQ = labels.Select((_, i) => preguntas.Count(d =>
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == allDates[i])).ToList();
            var respQ = labels.Select((_, i) => respuestas.Count(d =>
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                    d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == allDates[i])).ToList();

            return new JsonResult(new { ok = true, labels, preguntas = pregQ, respuestas = respQ }, _json);
        }

        // ── Chart: distribución de perfiles (dona) ──
        public IActionResult OnGetChartPerfiles()
        {
            return new JsonResult(new
            {
                ok = true,
                completos = Dto.PerfilesCompletos,
                basicos = Dto.PerfilesBasicos,
                minimos = Dto.PerfilesMinimos
            }, _json);
        }

        // ── Chart: top 10 países por nº usuarios ──
        public async Task<IActionResult> OnGetChartPaisesAsync()
        {
            var data = await _db.Perfil.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.NombrePais))
                .GroupBy(p => p.NombrePais)
                .Select(g => new { pais = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total)
                .Take(10)
                .ToListAsync();

            return new JsonResult(new { ok = true, data }, _json);
        }
    }
}
