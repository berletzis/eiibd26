using System;
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
    public class IngredientesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IngredientesModel(ApplicationDbContext db) => _db = db;

        public List<PlatIngrediente> Items { get; set; } = new();

        // Combos / checkboxes (alimentados desde catálogo, nunca texto a mano).
        public List<PlatGrupo> GrupoOptions { get; set; } = new();
        public List<PlatAtributo> AtributoOptions { get; set; } = new();   // intrínsecos (Ambito='Ingrediente')

        [BindProperty(SupportsGet = true)] public int? EditId { get; set; }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int GrupoId { get; set; }
        [BindProperty] public string? NotasEII { get; set; }
        [BindProperty] public List<int> SelectedAtributoIds { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Items = await _db.PlatIngredientes.AsNoTracking()
                .Include(i => i.Grupo)
                .Include(i => i.Atributos).ThenInclude(a => a.Atributo)
                .OrderBy(i => i.Nombre)
                .ToListAsync();

            // Combos: solo activos para registros nuevos. Si estamos editando, incluir además
            // la referencia ya asignada aunque esté inactiva (§7.2: no se pierde lo capturado).
            PlatIngrediente? editing = EditId.HasValue ? Items.FirstOrDefault(i => i.Id == EditId.Value) : null;

            GrupoOptions = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Activo)
                .OrderBy(g => g.Orden).ThenBy(g => g.Nombre)
                .ToListAsync();

            AtributoOptions = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Ingrediente")
                .OrderBy(a => a.Nombre)
                .ToListAsync();

            if (editing != null)
            {
                // Prefill del formulario
                Id = editing.Id;
                Nombre = editing.Nombre;
                GrupoId = editing.GrupoId;
                NotasEII = editing.NotasEII;
                SelectedAtributoIds = editing.Atributos.Select(x => x.AtributoId).ToList();

                // §7.2: incluir el grupo asignado aunque esté inactivo
                if (!GrupoOptions.Any(g => g.Id == editing.GrupoId) && editing.Grupo != null)
                    GrupoOptions.Add(editing.Grupo);

                // §7.2: incluir los atributos ya asignados aunque estén inactivos
                var assignedInactive = editing.Atributos
                    .Select(x => x.Atributo)
                    .Where(a => a != null && !AtributoOptions.Any(o => o.Id == a!.Id))
                    .Select(a => a!)
                    .ToList();
                AtributoOptions.AddRange(assignedInactive);
                AtributoOptions = AtributoOptions.OrderBy(a => a.Nombre).ToList();
            }
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            // Nombre limpio, minúscula, singular (§7.1). Minúscula la forzamos; singular es editorial.
            var nombre = (Nombre ?? "").Trim().ToLowerInvariant();
            var selected = (SelectedAtributoIds ?? new List<int>()).Distinct().ToList();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            var grupoOk = await _db.PlatGrupos.AnyAsync(g => g.Id == GrupoId);
            if (!grupoOk)
            {
                ErrorMessage = "Debes seleccionar un grupo válido.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            var dup = await _db.PlatIngredientes.AnyAsync(x => x.Nombre.ToLower() == nombre && x.Id != Id);
            if (dup)
            {
                ErrorMessage = $"Ya existe un ingrediente llamado \"{nombre}\". Revisa el catálogo antes de crear un duplicado.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            if (Id > 0)
            {
                var ent = await _db.PlatIngredientes.FirstOrDefaultAsync(x => x.Id == Id);
                if (ent == null) { ErrorMessage = "Ingrediente no encontrado."; return RedirectToPage(); }
                ent.Nombre = nombre;
                ent.GrupoId = GrupoId;
                ent.NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII!.Trim();
                await _db.SaveChangesAsync();
                await SyncAtributosAsync(ent.Id, selected);
                SuccessMessage = "Ingrediente actualizado.";
            }
            else
            {
                var ent = new PlatIngrediente
                {
                    Nombre = nombre,
                    GrupoId = GrupoId,
                    NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII!.Trim(),
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };
                _db.PlatIngredientes.Add(ent);
                await _db.SaveChangesAsync();
                await SyncAtributosAsync(ent.Id, selected);
                SuccessMessage = "Ingrediente creado.";
            }
            return RedirectToPage();
        }

        // Atributos intrínsecos: contenido de autoría → delete-all + insert de los seleccionados.
        private async Task SyncAtributosAsync(int ingredienteId, List<int> atributoIds)
        {
            var existentes = await _db.PlatIngredienteAtributos
                .Where(x => x.IngredienteId == ingredienteId)
                .ToListAsync();
            _db.PlatIngredienteAtributos.RemoveRange(existentes);

            foreach (var attrId in atributoIds)
            {
                _db.PlatIngredienteAtributos.Add(new PlatIngredienteAtributo
                {
                    IngredienteId = ingredienteId,
                    AtributoId = attrId
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id)
        {
            var ent = await _db.PlatIngredientes.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Ingrediente no encontrado."; return RedirectToPage(); }
            ent.Activo = !ent.Activo;
            await _db.SaveChangesAsync();
            SuccessMessage = ent.Activo ? "Ingrediente reactivado." : "Ingrediente desactivado.";
            return RedirectToPage();
        }
    }
}
