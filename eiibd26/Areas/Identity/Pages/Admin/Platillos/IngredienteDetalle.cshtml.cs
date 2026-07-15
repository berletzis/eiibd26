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
    /// <summary>
    /// Alta y edición de un ingrediente, en página full-page (se abre en pestaña aparte desde el
    /// grid). Con id → editar; sin id → alta. Misma lógica de guardado que vivía en Ingredientes:
    /// solo cambió dónde vive, no qué hace — incluido §7.2 y el delete-all+insert de atributos.
    /// La NOTA CLÍNICA no se edita aquí — tiene su propia página (NotaClinicaDetalle), porque es
    /// contenido que ve el paciente y se publica aparte.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class IngredienteDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IngredienteDetalleModel(ApplicationDbContext db) => _db = db;

        // Combos / checkboxes (alimentados desde catálogo, nunca texto a mano).
        public List<PlatGrupo> GrupoOptions { get; set; } = new();
        public List<PlatAtributo> AtributoOptions { get; set; } = new();   // intrínsecos (Ambito='Ingrediente')

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int GrupoId { get; set; }
        [BindProperty] public string? NotasEII { get; set; }
        [BindProperty] public List<int> SelectedAtributoIds { get; set; } = new();

        /// <summary>Solo informativo: la baja lógica se hace desde el grid.</summary>
        public bool Activo { get; private set; } = true;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            PlatIngrediente? editing = null;
            if (id.HasValue)
            {
                editing = await _db.PlatIngredientes.AsNoTracking()
                    .Include(i => i.Grupo)
                    .Include(i => i.Atributos).ThenInclude(a => a.Atributo)
                    .FirstOrDefaultAsync(i => i.Id == id.Value);
                if (editing == null)
                {
                    ErrorMessage = "Ingrediente no encontrado.";
                    return RedirectToPage("Ingredientes");
                }

                Id = editing.Id;
                Nombre = editing.Nombre;
                GrupoId = editing.GrupoId;
                NotasEII = editing.NotasEII;
                Activo = editing.Activo;
                SelectedAtributoIds = editing.Atributos.Select(x => x.AtributoId).ToList();
            }

            await CargarOpcionesAsync(editing);
            return Page();
        }

        /// <summary>
        /// Combos: solo activos para registros nuevos. Si estamos editando, incluir además la
        /// referencia ya asignada aunque esté inactiva (§7.2: no se pierde lo capturado).
        /// </summary>
        private async Task CargarOpcionesAsync(PlatIngrediente? editing)
        {
            GrupoOptions = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Activo)
                .OrderBy(g => g.Orden).ThenBy(g => g.Nombre)
                .ToListAsync();

            AtributoOptions = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Ingrediente")
                .OrderBy(a => a.Nombre)
                .ToListAsync();

            if (editing == null) return;

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

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            // Nombre limpio, minúscula, singular (§7.1). Minúscula la forzamos; singular es editorial.
            var nombre = (Nombre ?? "").Trim().ToLowerInvariant();
            var selected = (SelectedAtributoIds ?? new List<int>()).Distinct().ToList();
            var currentId = Id ?? 0;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { id = Id });
            }

            var grupoOk = await _db.PlatGrupos.AnyAsync(g => g.Id == GrupoId);
            if (!grupoOk)
            {
                ErrorMessage = "Debes seleccionar un grupo válido.";
                return RedirectToPage(new { id = Id });
            }

            var dup = await _db.PlatIngredientes.AnyAsync(x => x.Nombre.ToLower() == nombre && x.Id != currentId);
            if (dup)
            {
                ErrorMessage = $"Ya existe un ingrediente llamado \"{nombre}\". Revisa el catálogo antes de crear un duplicado.";
                return RedirectToPage(new { id = Id });
            }

            if (currentId > 0)
            {
                var ent = await _db.PlatIngredientes.FirstOrDefaultAsync(x => x.Id == currentId);
                if (ent == null)
                {
                    ErrorMessage = "Ingrediente no encontrado.";
                    return RedirectToPage("Ingredientes");
                }
                ent.Nombre = nombre;
                ent.GrupoId = GrupoId;
                ent.NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII!.Trim();
                await _db.SaveChangesAsync();
                await SyncAtributosAsync(ent.Id, selected);
                SuccessMessage = "Ingrediente actualizado.";
                return RedirectToPage(new { id = ent.Id });
            }

            var nuevo = new PlatIngrediente
            {
                Nombre = nombre,
                GrupoId = GrupoId,
                NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII!.Trim(),
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };
            _db.PlatIngredientes.Add(nuevo);
            await _db.SaveChangesAsync();
            await SyncAtributosAsync(nuevo.Id, selected);
            SuccessMessage = "Ingrediente creado.";
            return RedirectToPage(new { id = nuevo.Id });
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
    }
}
