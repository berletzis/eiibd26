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
    public class AtributosModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public AtributosModel(ApplicationDbContext db) => _db = db;

        // Ámbitos válidos (vocabulario controlado). El combo se alimenta de aquí.
        public static readonly string[] Ambitos = { "Ingrediente", "Uso" };

        public List<PlatAtributo> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int? EditId { get; set; }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public string? Ambito { get; set; }
        [BindProperty] public string? Descripcion { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (EditId.HasValue)
            {
                var ent = Items.FirstOrDefault(x => x.Id == EditId.Value);
                if (ent != null) { Id = ent.Id; Nombre = ent.Nombre; Ambito = ent.Ambito; Descripcion = ent.Descripcion; }
            }
        }

        private async Task LoadAsync()
        {
            Items = await _db.PlatAtributos.AsNoTracking()
                .OrderBy(x => x.Ambito).ThenBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var nombre = (Nombre ?? "").Trim();
            var ambito = (Ambito ?? "").Trim();
            var descripcion = (Descripcion ?? "").Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }
            if (!Ambitos.Contains(ambito))
            {
                ErrorMessage = "El ámbito debe ser 'Ingrediente' o 'Uso'.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            var dup = await _db.PlatAtributos.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != Id);
            if (dup)
            {
                ErrorMessage = $"Ya existe un atributo llamado \"{nombre}\".";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            if (Id > 0)
            {
                var ent = await _db.PlatAtributos.FirstOrDefaultAsync(x => x.Id == Id);
                if (ent == null) { ErrorMessage = "Atributo no encontrado."; return RedirectToPage(); }
                ent.Nombre = nombre;
                ent.Ambito = ambito;
                ent.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;
                await _db.SaveChangesAsync();
                SuccessMessage = "Atributo actualizado.";
            }
            else
            {
                _db.PlatAtributos.Add(new PlatAtributo
                {
                    Nombre = nombre,
                    Ambito = ambito,
                    Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                    Activo = true
                });
                await _db.SaveChangesAsync();
                SuccessMessage = "Atributo creado.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id)
        {
            var ent = await _db.PlatAtributos.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Atributo no encontrado."; return RedirectToPage(); }
            ent.Activo = !ent.Activo;
            await _db.SaveChangesAsync();
            SuccessMessage = ent.Activo ? "Atributo reactivado." : "Atributo desactivado.";
            return RedirectToPage();
        }
    }
}
