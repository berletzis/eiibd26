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
    /// Alta y edición de una categoría, en página full-page (se abre en pestaña aparte desde el grid).
    /// Con id → editar; sin id → alta. Misma lógica de guardado que vivía en Categorias: solo cambió
    /// dónde vive, no qué hace. Patrón UnidadDetalle.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class CategoriaDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CategoriaDetalleModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int Orden { get; set; }

        /// <summary>Solo informativo: la baja lógica se hace desde el grid.</summary>
        public bool Activo { get; private set; } = true;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var ent = await _db.PlatCategorias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (ent == null)
                {
                    ErrorMessage = "Categoría no encontrada.";
                    return RedirectToPage("Categorias");
                }
                Id = ent.Id;
                Nombre = ent.Nombre;
                Orden = ent.Orden;
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
            var dup = await _db.PlatCategorias.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != currentId);
            if (dup)
            {
                ErrorMessage = $"Ya existe una categoría llamada \"{nombre}\".";
                return RedirectToPage(new { id = Id });
            }

            if (currentId > 0)
            {
                var ent = await _db.PlatCategorias.FirstOrDefaultAsync(x => x.Id == currentId);
                if (ent == null)
                {
                    ErrorMessage = "Categoría no encontrada.";
                    return RedirectToPage("Categorias");
                }
                ent.Nombre = nombre;
                ent.Orden = Orden;
                await _db.SaveChangesAsync();
                SuccessMessage = "Categoría actualizada.";
                return RedirectToPage(new { id = ent.Id });
            }

            var nueva = new PlatCategoria { Nombre = nombre, Orden = Orden, Activo = true };
            _db.PlatCategorias.Add(nueva);
            await _db.SaveChangesAsync();
            SuccessMessage = "Categoría creada.";
            return RedirectToPage(new { id = nueva.Id });
        }
    }
}
