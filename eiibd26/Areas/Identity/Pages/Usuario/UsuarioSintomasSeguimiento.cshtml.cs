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
    public class UsuarioSintomasSeguimientoModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public UsuarioSintomasSeguimientoModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<SintomaSeguimiento> Sintomas { get; set; } = new();
        public List<DateTime> DiasSemana { get; set; } = new();
        public DateTime Hoy => DateTime.Now.Date;
        public DateTime Ayer => DateTime.Now.Date.AddDays(-1);

        public class SintomaSeguimiento
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public List<string> Condiciones { get; set; } = new();
            public Dictionary<string, string> SeguimientoPorDia { get; set; } = new(); // clave: yyyy-MM-dd, valor: estado/valor
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
                                         select new { su.id, s.nombre }).ToListAsync();

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

                var segPorDia = new Dictionary<string, string>();
                foreach (var dia in DiasSemana)
                {
                    var found = trackings.FirstOrDefault(t => t.IdSintomaUsuario == su.id && t.Fecha.Date == dia.Date);
                    segPorDia[dia.ToString("yyyy-MM-dd")] = found?.Estado ?? "";
                }
                return new SintomaSeguimiento
                {
                    Id = su.id,
                    Nombre = su.nombre,
                    Condiciones = condiciones,
                    SeguimientoPorDia = segPorDia
                };
            }).ToList();
        }

        // Handler para tracking desde la matriz, recibe fecha como texto para evitar error de formato
        public async Task<IActionResult> OnPostTrackSintomaMatrizAsync(int sintomaUsuarioId, string estado, string fecha)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();
            if (string.IsNullOrWhiteSpace(estado)) return BadRequest();

            var existeSintoma = await _db.sintomasUsuario.AnyAsync(x => x.id == sintomaUsuarioId && x.idUsuario == Guid.Parse(userId) && !x.Eliminado);
            if (!existeSintoma) return BadRequest();

            DateTime fechaParseada;
            if (!DateTime.TryParse(fecha, out fechaParseada))
                return BadRequest("Fecha inválida");

            // Si ya hay un tracking para ese día, elimínalo antes de crear uno nuevo
            var trackingExistente = await _db.TrackingSintomaUsuario
                .FirstOrDefaultAsync(t => t.IdSintomaUsuario == sintomaUsuarioId && t.IdUsuario == Guid.Parse(userId) && t.Fecha.Date == fechaParseada.Date);

            if (trackingExistente != null)
            {
                _db.TrackingSintomaUsuario.Remove(trackingExistente);
            }

            var tracking = new TrackingSintomaUsuario
            {
                IdUsuario = Guid.Parse(userId),
                IdSintomaUsuario = sintomaUsuarioId,
                Fecha = fechaParseada.Date + DateTime.Now.TimeOfDay,
                Estado = estado
            };
            _db.TrackingSintomaUsuario.Add(tracking);
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }
    }
}