using System.Collections.Generic;
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
    /// Listado de categorías — solo la lista (banco de trabajo). El alta y la edición viven en
    /// CategoriaDetalle, que se abre en pestaña aparte. Aquí queda la baja lógica, que es una
    /// acción de la propia lista.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class CategoriasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CategoriasModel(ApplicationDbContext db) => _db = db;

        public List<PlatCategoria> Items { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync() => await LoadAsync();

        private async Task LoadAsync()
        {
            Items = await _db.PlatCategorias.AsNoTracking()
                .OrderBy(x => x.Orden).ThenBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id)
        {
            var ent = await _db.PlatCategorias.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Categoría no encontrada."; return RedirectToPage(); }
            ent.Activo = !ent.Activo;
            await _db.SaveChangesAsync();
            SuccessMessage = ent.Activo ? "Categoría reactivada." : "Categoría desactivada.";
            return RedirectToPage();
        }
    }
}
