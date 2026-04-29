using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eiibd26.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return Unauthorized();
            var userGuid = Guid.Parse(userIdClaim);

            // 1) Moods - últimos 30 días (consistente con PDF "Último mes" y perfil público)
            var desde = DateTime.Today.AddDays(-30);
            var moods = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == userGuid && x.FechaRegistro >= desde && !x.Eliminado)
                .OrderBy(x => x.FechaRegistro)
                .Select(x => new MoodPoint
                {
                    Fecha = x.FechaRegistro,
                //    Estado = x.EstadoMood,
                    Texto = x.Texto,
                    RelacionNombre = x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre : (x.SintomaUsuario != null ? x.SintomaUsuario.Sintoma.nombre : null)
                })
                .ToListAsync();

            // 2) Relaciones del usuario (para modal agregar mood)
            var relaciones = await _db.condicionUsuario
                .Where(c => c.idUsuario == userGuid && !c.Eliminado)
                .Include(c => c.Condicion)
                .Select(c => new RelationItem { Id = c.id, Nombre = c.Condicion.nombre, Tipo = "condicion" })
                .ToListAsync();

            var sintomasUsuario = await _db.sintomasUsuario
                .Where(s => s.idUsuario == userGuid && !s.Eliminado)
                .Include(s => s.Sintoma)
                .Select(s => new RelationItem { Id = s.id, Nombre = s.Sintoma.nombre, Tipo = "sintoma" })
                .ToListAsync();

            relaciones.AddRange(sintomasUsuario);

            var tratamientosUsuario = await _db.tratamientoUsuario
                .Where(t => t.idUsuario == userGuid && !t.Eliminado)
                .Include(t => t.Tratamiento)
                .Select(t => new RelationItem { Id = t.id, Nombre = t.Tratamiento.nombre, Tipo = "tratamiento" })
                .ToListAsync();

            relaciones.AddRange(tratamientosUsuario);

            // 3) Top 3 síntomas por interacción (TrackingSintomaUsuario.IdSintomaUsuario) - últimos 30 días
            var topSintomas = await _db.TrackingSintomaUsuario
                .Where(t => t.IdUsuario == userGuid && t.Fecha >= desde)
                .GroupBy(t => t.IdSintomaUsuario)
                .Select(g => new { IdSintomaUsuario = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(3)
                .Join(_db.sintomasUsuario, g => g.IdSintomaUsuario, su => su.id,
                    (g, su) => new { g.IdSintomaUsuario, g.Count, su.idSintoma })
                .Join(_db.sintomas, temp => temp.idSintoma, s => s.id,
                    (temp, s) => new SymptomTopItem { SintomaUsuarioId = temp.IdSintomaUsuario, Nombre = s.nombre, Interacciones = temp.Count })
                .ToListAsync();

            // 4) Mis preguntas: obtener top 3 por votos
            var preguntasConVotos = await _db.Preguntas
                .Where(p => p.UsuarioId == userGuid)
                .Select(p => new
                {
                    p.Id,
                    p.Titulo,
                    AnswersCount = p.Respuestas.Count(),
                    Votes = _db.Votos.Where(v => v.EntidadTipo == "pregunta" && v.EntidadId == p.Id).Select(v => (int?)v.Valor).Sum() ?? 0,
                    p.FechaCreacion
                })
                .OrderByDescending(p => p.Votes)
                .ThenByDescending(p => p.FechaCreacion)
                .Take(3)
                .ToListAsync();

            var preguntas = preguntasConVotos.Select(p => new QuestionItem
            {
                Id = p.Id,
                Titulo = p.Titulo,
                AnswersCount = p.AnswersCount,
                Votes = p.Votes
            }).ToList();

            // 5) Mis respuestas: top 3 por votos
            var respuestasConVotos = await _db.Respuestas
                .Where(r => r.UsuarioId == userGuid)
                .Select(r => new
                {
                    r.Id,
                    Cuerpo = r.Cuerpo,
                    Votes = _db.Votos.Where(v => v.EntidadTipo == "respuesta" && v.EntidadId == r.Id).Select(v => (int?)v.Valor).Sum() ?? 0,
                    r.FechaCreacion,
                    r.PreguntaId
                })
                .OrderByDescending(r => r.Votes)
                .ThenByDescending(r => r.FechaCreacion)
                .Take(3)
                .ToListAsync();

            var respuestas = respuestasConVotos.Select(r => new AnswerItem
            {
                Id = r.Id,
                Cuerpo = r.Cuerpo,
                Votes = r.Votes,
                PreguntaId = r.PreguntaId
            }).ToList();

            // ===== Nuevas flags / contadores para la tarjeta de notificaciones =====

            // Email / teléfono: obtener usuario de Identity para consultar flags
            var appUser = await _userManager.GetUserAsync(User);
            bool emailConfirmed = false;
            bool phoneConfirmed = false;
            if (appUser != null)
            {
                emailConfirmed = await _userManager.IsEmailConfirmedAsync(appUser);
                // Temporalmente deshabilitamos la notificación de teléfono (ver vista parcial)
                // si prefieres mantener el valor real descomenta la línea siguiente:
                // phoneConfirmed = await _userManager.IsPhoneNumberConfirmedAsync(appUser);
                phoneConfirmed = true; // marcar true para ocultar notificación por el momento
            }

            // HasAnyCondition: true si el usuario tiene al menos una condicionUsuario no eliminada (cualquier momento)
            var hasAnyCondition = await _db.condicionUsuario.AnyAsync(c => c.idUsuario == userGuid && !c.Eliminado);

            // HasMoodToday: comprobamos si existe un estado de ánimo para el día actual (UTC)
            var startUtc = DateTime.UtcNow.Date;
            var endUtc = startUtc.AddDays(1);
            var hasMoodToday = await _db.EstadoAnimoUsuario
                .AnyAsync(e => e.IdUsuario == userGuid && !e.Eliminado && e.FechaRegistro >= startUtc && e.FechaRegistro < endUtc);

            // NewAnswersCount: respuestas a mis preguntas en los últimos 7 días
            var since = DateTimeOffset.UtcNow.AddDays(-7);
            var userQuestionIds = await _db.Preguntas.Where(p => p.UsuarioId == userGuid).Select(p => p.Id).ToListAsync();
            int newAnswersCount = 0;
            if (userQuestionIds.Any())
            {
                newAnswersCount = await _db.Respuestas
                    .Where(r => userQuestionIds.Contains(r.PreguntaId) && !r.Eliminado && r.FechaCreacion >= since)
                    .CountAsync();
            }

            // ScheduledItemsCount: placeholder (0) - implementar según tu modelo de "programados" cuando exista
            int scheduledItemsCount = 0;

            var model = new DashboardViewModel
            {
                Moods = moods,
                //MoodRelations = relaciones,
                TopSintomas = topSintomas,
                Preguntas = preguntas,
                Respuestas = respuestas,
                EmailConfirmed = emailConfirmed,
                PhoneNumberConfirmed = phoneConfirmed,
                HasAnyCondition = hasAnyCondition,
                HasMoodToday = hasMoodToday,
                NewAnswersCount = newAnswersCount,
                ScheduledItemsCount = scheduledItemsCount
            };

            return View(model);
        }

        [HttpPost("add-mood")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMood([FromForm] string mood, [FromForm] string texto, [FromForm] int? relacionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return Unauthorized();
            var userGuid = Guid.Parse(userIdClaim);

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = userGuid,
                //EstadoMood = mood,
                Texto = string.IsNullOrWhiteSpace(texto) ? null : texto,
                FechaRegistro = DateTime.UtcNow,
                IdCondicionUsuario = relacionId
            };
            _db.EstadoAnimoUsuario.Add(nuevo);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("add-sintoma")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSymptom([FromForm] int sintomaUsuarioId, [FromForm] string estado = "Ninguno")
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return Unauthorized();
            var userGuid = Guid.Parse(userIdClaim);

            // Registrar una interacción en TrackingSintomaUsuario (usa Fecha y Estado)
            var tracking = new TrackingSintomaUsuario
            {
                IdUsuario = userGuid,
                IdSintomaUsuario = sintomaUsuarioId,
                Fecha = DateTime.UtcNow,
                Estado = string.IsNullOrWhiteSpace(estado) ? "Ninguno" : estado
            };

            _db.TrackingSintomaUsuario.Add(tracking);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }

    public class RelationItem
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
    }
}