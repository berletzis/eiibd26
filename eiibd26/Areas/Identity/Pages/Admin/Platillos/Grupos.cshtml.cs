using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using eiibd26.Services.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    /// <summary>
    /// Listado de grupos — solo la lista (banco de trabajo). El alta y la edición viven en
    /// GrupoDetalle, y la nota clínica en NotaClinicaDetalle; ambas se abren en pestaña aparte.
    /// Aquí queda la baja lógica (acción de la propia lista) y la columna de estado de la nota.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    public class GruposModel : PageModel
    {
        private const string TipoDestino = "Grupo";

        private readonly ApplicationDbContext _db;
        private readonly IPlatNotaAdminService _notas;
        public GruposModel(ApplicationDbContext db, IPlatNotaAdminService notas)
        {
            _db = db;
            _notas = notas;
        }

        public List<PlatGrupo> Items { get; set; } = new();

        /// <summary>Estado de la nota clínica por GrupoId (columna de la lista).</summary>
        public Dictionary<int, PlatNotaEstado> NotaEstados { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync() => await LoadAsync();

        private async Task LoadAsync()
        {
            Items = await _db.PlatGrupos.AsNoTracking()
                .OrderBy(x => x.Orden).ThenBy(x => x.Nombre)
                .ToListAsync();

            // Estado de nota por grupo (Sin nota / Borrador / Publicada), en una sola consulta.
            NotaEstados = await _notas.ObtenerEstadosAsync(TipoDestino);
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id)
        {
            var ent = await _db.PlatGrupos.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Grupo no encontrado."; return RedirectToPage(); }
            ent.Activo = !ent.Activo;
            await _db.SaveChangesAsync();
            SuccessMessage = ent.Activo ? "Grupo reactivado." : "Grupo desactivado.";
            return RedirectToPage();
        }
    }
}
