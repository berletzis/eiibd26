using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    /// <summary>
    /// Alta y edición de una unidad, en página full-page (se abre en pestaña aparte desde el grid).
    /// Con id → editar; sin id → alta. Misma lógica de guardado que vivía en Unidades; aquí solo
    /// cambió dónde vive, no qué hace. Tras guardar (PRG) la página se queda con el mensaje de
    /// éxito y el enlace "Volver al listado": no se intenta auto-cerrar la pestaña.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class UnidadDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UnidadDetalleModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }

        /// <summary>Solo informativo: el alta/baja lógica se hace desde el grid.</summary>
        public bool Activo { get; private set; } = true;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var ent = await _db.PlatUnidades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (ent == null)
                {
                    ErrorMessage = "Unidad no encontrada.";
                    return RedirectToPage("Unidades");
                }
                Id = ent.Id;
                Nombre = ent.Nombre;
                Activo = ent.Activo;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var nombre = (Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { id = Id });
            }

            var currentId = Id ?? 0;
            var dup = await _db.PlatUnidades.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != currentId);
            if (dup)
            {
                ErrorMessage = $"Ya existe una unidad llamada \"{nombre}\".";
                return RedirectToPage(new { id = Id });
            }

            if (currentId > 0)
            {
                var ent = await _db.PlatUnidades.FirstOrDefaultAsync(x => x.Id == currentId);
                if (ent == null)
                {
                    ErrorMessage = "Unidad no encontrada.";
                    return RedirectToPage("Unidades");
                }
                ent.Nombre = nombre;
                await _db.SaveChangesAsync();
                SuccessMessage = "Unidad actualizada.";
                return RedirectToPage(new { id = ent.Id });
            }

            var nueva = new PlatUnidad { Nombre = nombre, Activo = true };
            _db.PlatUnidades.Add(nueva);
            await _db.SaveChangesAsync();
            SuccessMessage = "Unidad creada.";
            return RedirectToPage(new { id = nueva.Id });
        }
    }
}
