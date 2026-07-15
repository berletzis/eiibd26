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
    /// Alta y edición de un atributo, en página full-page (se abre en pestaña aparte desde el grid).
    /// Con id → editar; sin id → alta. Misma lógica de guardado que vivía en Atributos: solo cambió
    /// dónde vive, no qué hace. Patrón UnidadDetalle.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class AtributoDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public AtributoDetalleModel(ApplicationDbContext db) => _db = db;

        /// <summary>
        /// Ámbitos válidos (vocabulario controlado). El combo se alimenta de aquí. Vivía en
        /// AtributosModel, pero el grid dejó de tener combo y validación → se mudó a su único
        /// consumidor. NO es un catálogo en BD: no tiene Activo, así que no aplica el §7.2.
        /// </summary>
        public static readonly string[] Ambitos = { "Ingrediente", "Uso" };

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public string? Ambito { get; set; }
        [BindProperty] public string? Descripcion { get; set; }

        /// <summary>Solo informativo: la baja lógica se hace desde el grid.</summary>
        public bool Activo { get; private set; } = true;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var ent = await _db.PlatAtributos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (ent == null)
                {
                    ErrorMessage = "Atributo no encontrado.";
                    return RedirectToPage("Atributos");
                }
                Id = ent.Id;
                Nombre = ent.Nombre;
                Ambito = ent.Ambito;
                Descripcion = ent.Descripcion;
                Activo = ent.Activo;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var nombre = (Nombre ?? "").Trim();
            var ambito = (Ambito ?? "").Trim();
            var descripcion = (Descripcion ?? "").Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { id = Id });
            }
            if (!Ambitos.Contains(ambito))
            {
                ErrorMessage = "El ámbito debe ser 'Ingrediente' o 'Uso'.";
                return RedirectToPage(new { id = Id });
            }

            var currentId = Id ?? 0;
            var dup = await _db.PlatAtributos.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != currentId);
            if (dup)
            {
                ErrorMessage = $"Ya existe un atributo llamado \"{nombre}\".";
                return RedirectToPage(new { id = Id });
            }

            if (currentId > 0)
            {
                var ent = await _db.PlatAtributos.FirstOrDefaultAsync(x => x.Id == currentId);
                if (ent == null)
                {
                    ErrorMessage = "Atributo no encontrado.";
                    return RedirectToPage("Atributos");
                }
                ent.Nombre = nombre;
                ent.Ambito = ambito;
                ent.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;
                await _db.SaveChangesAsync();
                SuccessMessage = "Atributo actualizado.";
                return RedirectToPage(new { id = ent.Id });
            }

            var nuevo = new PlatAtributo
            {
                Nombre = nombre,
                Ambito = ambito,
                Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                Activo = true
            };
            _db.PlatAtributos.Add(nuevo);
            await _db.SaveChangesAsync();
            SuccessMessage = "Atributo creado.";
            return RedirectToPage(new { id = nuevo.Id });
        }
    }
}
