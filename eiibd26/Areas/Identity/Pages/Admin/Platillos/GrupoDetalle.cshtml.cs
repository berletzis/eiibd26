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
    /// Alta y edición de un grupo, en página full-page (se abre en pestaña aparte desde el grid).
    /// Con id → editar; sin id → alta. Misma lógica de guardado que vivía en Grupos: solo cambió
    /// dónde vive, no qué hace. La NOTA CLÍNICA no se edita aquí — tiene su propia página
    /// (NotaClinicaDetalle), porque es contenido que ve el paciente y se publica aparte.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class GrupoDetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public GrupoDetalleModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public int? Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int Orden { get; set; }
        [BindProperty] public string? NotasEII { get; set; }
        /// <summary>Anexo 5: tipo de riesgo del grupo (null/vacío = sin riesgo). Habilita la nota de precaución.</summary>
        [BindProperty] public string? RiesgoTipo { get; set; }

        /// <summary>Solo informativo: la baja lógica se hace desde el grid.</summary>
        public bool Activo { get; private set; } = true;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                var ent = await _db.PlatGrupos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (ent == null)
                {
                    ErrorMessage = "Grupo no encontrado.";
                    return RedirectToPage("Grupos");
                }
                Id = ent.Id;
                Nombre = ent.Nombre;
                Orden = ent.Orden;
                NotasEII = ent.NotasEII;
                RiesgoTipo = ent.RiesgoTipo;
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
            var dup = await _db.PlatGrupos.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != currentId);
            if (dup)
            {
                ErrorMessage = $"Ya existe un grupo llamado \"{nombre}\".";
                return RedirectToPage(new { id = Id });
            }

            if (currentId > 0)
            {
                var ent = await _db.PlatGrupos.FirstOrDefaultAsync(x => x.Id == currentId);
                if (ent == null)
                {
                    ErrorMessage = "Grupo no encontrado.";
                    return RedirectToPage("Grupos");
                }
                ent.Nombre = nombre;
                ent.Orden = Orden;
                ent.NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII.Trim();
                ent.RiesgoTipo = string.IsNullOrWhiteSpace(RiesgoTipo) ? null : RiesgoTipo.Trim();
                await _db.SaveChangesAsync();
                SuccessMessage = "Grupo actualizado.";
                return RedirectToPage(new { id = ent.Id });
            }

            var nuevo = new PlatGrupo
            {
                Nombre = nombre,
                Orden = Orden,
                Activo = true,
                NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII.Trim(),
                RiesgoTipo = string.IsNullOrWhiteSpace(RiesgoTipo) ? null : RiesgoTipo.Trim()
            };
            _db.PlatGrupos.Add(nuevo);
            await _db.SaveChangesAsync();
            SuccessMessage = "Grupo creado.";
            return RedirectToPage(new { id = nuevo.Id });
        }
    }
}
