using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

        // Estado de la nota clínica por GrupoId (columna de la lista).
        public Dictionary<int, PlatNotaEstado> NotaEstados { get; set; } = new();
        // Panel lateral de nota: no null cuando NotaId trae un grupo a editar.
        public PlatNotaEditVm? NotaEdit { get; set; }

        [BindProperty(SupportsGet = true)] public int? EditId { get; set; }
        [BindProperty(SupportsGet = true)] public int? NotaId { get; set; }

        [BindProperty] public int Id { get; set; }
        [BindProperty] public string? Nombre { get; set; }
        [BindProperty] public int Orden { get; set; }
        [BindProperty] public string? NotasEII { get; set; }

        // Binding del formulario de nota (panel lateral).
        [BindProperty] public int NotaDestinoId { get; set; }
        [BindProperty] public string? NotaTitulo { get; set; }
        [BindProperty] public List<PlatNotaSeccionInput> NotaSecciones { get; set; } = new();
        [BindProperty] public List<PlatNotaReferenciaInput> NotaReferencias { get; set; } = new();

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
            if (EditId.HasValue)
            {
                var ent = Items.FirstOrDefault(x => x.Id == EditId.Value);
                if (ent != null) { Id = ent.Id; Nombre = ent.Nombre; Orden = ent.Orden; NotasEII = ent.NotasEII; }
            }
            if (NotaId.HasValue)
            {
                var g = Items.FirstOrDefault(x => x.Id == NotaId.Value);
                if (g != null) NotaEdit = await _notas.CargarAsync(TipoDestino, g.Id, g.Nombre);
            }
        }

        private async Task LoadAsync()
        {
            Items = await _db.PlatGrupos.AsNoTracking()
                .OrderBy(x => x.Orden).ThenBy(x => x.Nombre)
                .ToListAsync();

            // Estado de nota por grupo (Sin nota / Borrador / Publicada), en una sola consulta.
            NotaEstados = await _notas.ObtenerEstadosAsync(TipoDestino);
        }

        private Guid CurrentUserId() =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

        public async Task<IActionResult> OnPostGuardarAsync()
        {
            var nombre = (Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                ErrorMessage = "El nombre es obligatorio.";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            var dup = await _db.PlatGrupos.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower() && x.Id != Id);
            if (dup)
            {
                ErrorMessage = $"Ya existe un grupo llamado \"{nombre}\".";
                return RedirectToPage(new { editId = Id > 0 ? Id : (int?)null });
            }

            if (Id > 0)
            {
                var ent = await _db.PlatGrupos.FirstOrDefaultAsync(x => x.Id == Id);
                if (ent == null) { ErrorMessage = "Grupo no encontrado."; return RedirectToPage(); }
                ent.Nombre = nombre;
                ent.Orden = Orden;
                ent.NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII.Trim();
                await _db.SaveChangesAsync();
                SuccessMessage = "Grupo actualizado.";
            }
            else
            {
                _db.PlatGrupos.Add(new PlatGrupo { Nombre = nombre, Orden = Orden, Activo = true, NotasEII = string.IsNullOrWhiteSpace(NotasEII) ? null : NotasEII.Trim() });
                await _db.SaveChangesAsync();
                SuccessMessage = "Grupo creado.";
            }
            return RedirectToPage();
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

        // ---- Nota clínica (panel lateral). Reglas en PlatNotaAdminService; aquí handlers finos. ----

        public async Task<IActionResult> OnPostGuardarNotaAsync()
        {
            var (ok, msg) = await _notas.GuardarBorradorAsync(
                TipoDestino, NotaDestinoId, NotaTitulo, NotaSecciones, NotaReferencias);
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { notaId = NotaDestinoId });
        }

        public async Task<IActionResult> OnPostPublicarNotaAsync(int destinoId)
        {
            var (ok, msg) = await _notas.PublicarAsync(TipoDestino, destinoId, CurrentUserId());
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { notaId = destinoId });
        }

        public async Task<IActionResult> OnPostDespublicarNotaAsync(int destinoId)
        {
            var (ok, msg) = await _notas.DespublicarAsync(TipoDestino, destinoId);
            if (ok) SuccessMessage = msg; else ErrorMessage = msg;
            return RedirectToPage(new { notaId = destinoId });
        }
    }
}
