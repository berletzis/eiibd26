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
    [Authorize]
    public class UsuarioSintomasSeguimientoModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ITrackingSintomaService _trackingService;
        private readonly IHealthStatsService _healthStats;

        public UsuarioSintomasSeguimientoModel(ApplicationDbContext db, ITrackingSintomaService trackingService, IHealthStatsService healthStats)
        {
            _db = db;
            _trackingService = trackingService;
            _healthStats = healthStats;
        }

        /// <summary>Tendencia por síntoma, clave = NombreSintoma.Trim(). Sin datos = "Sin datos suficientes".</summary>
        public Dictionary<string, SymptomTrendDto> TendenciasPorSintoma { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public List<SintomaSeguimiento> Sintomas { get; set; } = new();
        public List<DateTime> DiasSemana { get; set; } = new();
        public List<FrecuenciaSintomaCatalog> FrecuenciasCatalog { get; set; } = new();
        public DateTime Hoy => DateTime.Now.Date;
        public DateTime Ayer => DateTime.Now.Date.AddDays(-1);

        public class DiaTracking
        {
            public string Estado { get; set; } = "";
            public int? Dolor { get; set; }
            public bool? TieneSangrado { get; set; }
            public int? FrecuenciaId { get; set; }
        }

        public class SintomaSeguimiento
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public int TipoSintoma { get; set; } = 0;
            public List<string> Condiciones { get; set; } = new();
            public Dictionary<string, DiaTracking> SeguimientoPorDia { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;
            var userGuid = Guid.Parse(userId);

            // 7 días: hoy y los 6 previos
            var hoy = DateTime.Now.Date;
            DiasSemana = Enumerable.Range(0, 7)
                .Select(d => hoy.AddDays(-6 + d)).ToList();

            // Síntomas del usuario
            var sintomasUsuario = await (from su in _db.sintomasUsuario
                                         join s in _db.sintomas on su.idSintoma equals s.id
                                         where su.idUsuario == userGuid && !su.Eliminado
                                         select new { su.id, s.nombre, s.TipoSintoma }).ToListAsync();

            // Condiciones asociadas por síntoma usuario
            var condicionesAll = await (
                from rel in _db.SintomaCondicionUsuario
                join cu in _db.condicionUsuario on rel.IdCondicionUsuario equals cu.id
                join c in _db.condiciones on cu.idCondicion equals c.id
                where cu.idUsuario == userGuid && !cu.Eliminado
                select new { rel.IdSintomaUsuario, Condicion = c.nombre }
            ).ToListAsync();

            // Trackings registrados SOLO para la ventana de la semana
            var desde = DiasSemana.First();
            var hasta = DiasSemana.Last().AddDays(1);
            var trackings = await _db.TrackingSintomaUsuario
                .Where(t => t.IdUsuario == userGuid && t.Fecha >= desde && t.Fecha < hasta)
                .ToListAsync();

            Sintomas = sintomasUsuario.Select(su =>
            {
                var condiciones = condicionesAll
                    .Where(c => c.IdSintomaUsuario == su.id)
                    .Select(c => c.Condicion).ToList();

                var segPorDia = new Dictionary<string, DiaTracking>();
                foreach (var dia in DiasSemana)
                {
                    var found = trackings.FirstOrDefault(t => t.IdSintomaUsuario == su.id && t.Fecha.Date == dia.Date);
                    segPorDia[dia.ToString("yyyy-MM-dd")] = found != null
                        ? new DiaTracking
                          {
                              Estado       = found.Estado ?? "",
                              Dolor        = found.Dolor,
                              TieneSangrado = found.TieneSangrado,
                              FrecuenciaId  = found.FrecuenciaId
                          }
                        : new DiaTracking();
                }
                return new SintomaSeguimiento
                {
                    Id = su.id,
                    Nombre = su.nombre,
                    TipoSintoma = su.TipoSintoma,
                    Condiciones = condiciones,
                    SeguimientoPorDia = segPorDia
                };
            }).ToList();

            FrecuenciasCatalog = await _db.FrecuenciaSintomaCatalog
                .OrderBy(f => f.Orden)
                .AsNoTracking()
                .ToListAsync();

            // Calcular tendencias usando trackings ya en memoria — sin nueva query
            var sintomasExport = MapearSintomasExport(sintomasUsuario
                .Select(su => (su.id, su.nombre)).ToList(), trackings);
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

        // Handler para tracking desde la matriz, recibe fecha como texto para evitar error de formato
        public async Task<IActionResult> OnPostTrackSintomaMatrizAsync(int sintomaUsuarioId, string estado, string fecha,
            int? dolor, bool? tieneSangrado, int? frecuenciaId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(estado)) return BadRequest();

            var existeSintoma = await _db.sintomasUsuario.AnyAsync(x => x.id == sintomaUsuarioId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);
            if (!existeSintoma) return BadRequest();

            DateTime fechaParseada;
            if (!DateTime.TryParse(fecha, out fechaParseada))
                return BadRequest("Fecha inválida (check)");

            var userGuid = Guid.Parse(userId);
            await _trackingService.GuardarTrackingAsync(new TrackingRequestDto(
                IdUsuario:        userGuid,
                IdSintomaUsuario: sintomaUsuarioId,
                Fecha:            fechaParseada,
                Estado:           estado,
                Dolor:            dolor.HasValue ? Math.Clamp(dolor.Value, 0, 10) : null,
                FrecuenciaId:     frecuenciaId > 0 ? frecuenciaId : null,
                TieneSangrado:    tieneSangrado
            ));
            return new JsonResult(new { ok = true });
        }
    }
}