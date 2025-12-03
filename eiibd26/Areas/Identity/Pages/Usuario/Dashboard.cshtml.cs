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

            // Cargar moods (últimos 50 registros) — el partial se encargará de filtrar por semana actual
            var moods = await _db.EstadoAnimoUsuario
                .Where(x => x.IdUsuario == userGuid)
                .OrderByDescending(x => x.FechaRegistro)
                .Take(50)
                .Select(x => new MoodPoint
                {
                    Fecha = x.FechaRegistro,
                    Estado = x.EstadoMood,
                    Texto = x.Texto,
                    RelacionNombre = x.CondicionUsuario != null ? x.CondicionUsuario.Condicion.nombre :
                                    (x.SintomaUsuario != null ? x.SintomaUsuario.Sintoma.nombre : null)
                })
                .ToListAsync();

            // Relaciones (opciones) para el modal
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

            VM.Moods = moods;
            VM.MoodRelations = relaciones;
        }

        // Nota: el modal hace POST vía AJAX a /api/EstadoAnimoUsuario/nuevo.
        // Se mantiene OnPostAddMoodAsync como fallback si se desea usar el submit server-side.
        public async Task<IActionResult> OnPostAddMoodAsync([FromForm] string mood, [FromForm] string texto, [FromForm] int? relacionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim)) return Challenge();
            if (!Guid.TryParse(userIdClaim, out var userGuid)) return Challenge();

            var nuevo = new EstadoAnimoUsuario
            {
                IdUsuario = userGuid,
                EstadoMood = mood,
                Texto = string.IsNullOrWhiteSpace(texto) ? null : texto,
                FechaRegistro = DateTime.Now,
                IdCondicionUsuario = relacionId
            };
            _db.EstadoAnimoUsuario.Add(nuevo);
            await _db.SaveChangesAsync();

            return RedirectToPage(); // PRG fallback
        }
    }
}