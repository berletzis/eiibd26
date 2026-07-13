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
    [Authorize(Roles = "Administrador")]
    public class UnidadesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UnidadesModel(ApplicationDbContext db) => _db = db;

        public List<PlatUnidad> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int? EditId { get; set; }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (EditId.HasValue)
            {
                var ent = Items.FirstOrDefault(x => x.Id == EditId.Value);
                if (ent != null) { Id = ent.Id; Nombre = ent.Nombre; }
            }
        }

        private async Task LoadAsync()
        {
            Items = await _db.PlatUnidades.AsNoTracking()
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var nombre = (Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            var dup = await _db.PlatUnidades.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != Id);
            if (dup)
            {
                ErrorMessage = $"Ya existe una unidad llamada \"{nombre}\".";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            if (Id > 0)
            {
                var ent = await _db.PlatUnidades.FirstOrDefaultAsync(x => x.Id == Id);
                if (ent == null) { ErrorMessage = "Unidad no encontrada."; return RedirectToPage(); }
                ent.Nombre = nombre;
                await _db.SaveChangesAsync();
                SuccessMessage = "Unidad actualizada.";
            }
            else
            {
                _db.PlatUnidades.Add(new PlatUnidad { Nombre = nombre, Activo = true });
                await _db.SaveChangesAsync();
                SuccessMessage = "Unidad creada.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id)
        {
            var ent = await _db.PlatUnidades.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Unidad no encontrada."; return RedirectToPage(); }
            ent.Activo = !ent.Activo;
            await _db.SaveChangesAsync();
            SuccessMessage = ent.Activo ? "Unidad reactivada." : "Unidad desactivada.";
            return RedirectToPage();
        }
    }
}
