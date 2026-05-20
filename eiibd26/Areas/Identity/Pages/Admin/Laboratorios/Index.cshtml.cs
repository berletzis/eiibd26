using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Laboratorios
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public void OnGet() { }

        // GET: Grid de tipos (catálogo)
        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw   = int.TryParse(Request.Query["draw"],   out var d) ? d : 1;
            var start  = int.TryParse(Request.Query["start"],  out var s) ? s : 0;
            var length = int.TryParse(Request.Query["length"], out var l) ? l : 10;
            var search = Request.Query["search[value]"].ToString();

            var all = await _db.LaboratoryTypes
                .IgnoreQueryFilters()
                .Where(t => mostrarEliminados || !t.Eliminado)
                .Where(t => string.IsNullOrEmpty(search) || t.Name.Contains(search))
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Slug,
                    t.Description,
                    t.IsActive,
                    t.ParentId,
                    t.Eliminado
                })
                .ToListAsync();

            // Ordenar: padres primero, luego hijos
            var result = new List<object>();
            var padres = all.Where(t => t.ParentId == null).OrderBy(t => t.Name).ToList();
            foreach (var padre in padres)
            {
                result.Add(new { padre.Id, padre.Name, padre.Slug, padre.Description, padre.IsActive, padre.ParentId, padre.Eliminado, esPadre = true });
                foreach (var hijo in all.Where(t => t.ParentId == padre.Id).OrderBy(t => t.Name))
                    result.Add(new { hijo.Id, hijo.Name, hijo.Slug, hijo.Description, hijo.IsActive, hijo.ParentId, hijo.Eliminado, esPadre = false });
            }

            var total = result.Count;
            return new JsonResult(new { draw, recordsTotal = total, recordsFiltered = total, data = result.Skip(start).Take(length) });
        }

        // GET: Obtener un tipo por id
        public async Task<IActionResult> OnGetGetTipoAsync(int id)
        {
            var t = await _db.LaboratoryTypes.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            return new JsonResult(new { t.Id, t.Name, t.Slug, t.Description, t.ParentId, t.IsActive, t.Eliminado });
        }

        // GET: Lista de padres para el select del modal
        public async Task<IActionResult> OnGetPadresAsync()
        {
            var padres = await _db.LaboratoryTypes.IgnoreQueryFilters()
                .Where(t => t.ParentId == null && !t.Eliminado)
                .Select(t => new { t.Id, t.Name })
                .OrderBy(t => t.Name)
                .ToListAsync();
            return new JsonResult(padres);
        }

        // POST: Crear tipo
        public async Task<IActionResult> OnPostCrearTipoAsync()
        {
            var name        = Request.Form["name"].ToString().Trim();
            var slug        = Request.Form["slug"].ToString().Trim();
            var description = Request.Form["description"].ToString().Trim();
            var isActive    = Request.Form["isActive"].ToString() != "false";
            var parentIdStr = Request.Form["parentId"].ToString();
            int? parentId   = string.IsNullOrWhiteSpace(parentIdStr) ? null : int.Parse(parentIdStr);

            if (string.IsNullOrWhiteSpace(name))
                return new JsonResult(new { success = false, message = "El nombre es obligatorio." });

            var nuevo = new LaboratoryType
            {
                Name        = name,
                Slug        = string.IsNullOrWhiteSpace(slug) ? null : slug,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                IsActive    = isActive,
                ParentId    = parentId,
                FechaCreado = DateTime.UtcNow,
                Eliminado   = false
            };

            _db.LaboratoryTypes.Add(nuevo);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // POST: Editar tipo
        public async Task<IActionResult> OnPostEditarTipoAsync()
        {
            if (!int.TryParse(Request.Form["id"], out var id))
                return new JsonResult(new { success = false, message = "ID inválido." });

            var t = await _db.LaboratoryTypes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
                return new JsonResult(new { success = false, message = "Tipo no encontrado." });

            var parentIdStr = Request.Form["parentId"].ToString();

            t.Name        = Request.Form["name"].ToString().Trim();
            t.Slug        = Request.Form["slug"].ToString().Trim().NullIfEmpty();
            t.Description = Request.Form["description"].ToString().Trim().NullIfEmpty();
            t.IsActive    = Request.Form["isActive"].ToString() != "false";
            t.ParentId    = string.IsNullOrWhiteSpace(parentIdStr) ? null : int.Parse(parentIdStr);
            t.Eliminado   = Request.Form["eliminado"].ToString() == "true";
            t.FechaModificado = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // POST: Eliminar (soft delete)
        public async Task<IActionResult> OnPostEliminarTipoAsync()
        {
            if (!int.TryParse(Request.Form["id"], out var id))
                return new JsonResult(new { success = false, message = "ID inválido." });

            var t = await _db.LaboratoryTypes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
                return new JsonResult(new { success = false, message = "Tipo no encontrado." });

            var tieneHijos = await _db.LaboratoryTypes.IgnoreQueryFilters().AnyAsync(x => x.ParentId == id && !x.Eliminado);
            if (tieneHijos)
                return new JsonResult(new { success = false, message = "No puedes eliminar un tipo que tiene subcategorías activas." });

            var tieneResultados = await _db.PatientLaboratoryResults.IgnoreQueryFilters().AnyAsync(x => x.LaboratoryTypeId == id && !x.Eliminado);
            if (tieneResultados)
                return new JsonResult(new { success = false, message = "No puedes eliminar un tipo que tiene resultados registrados." });

            t.Eliminado      = true;
            t.FechaEliminado = DateTime.UtcNow;
            t.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        // POST: Restaurar
        public async Task<IActionResult> OnPostRestaurarTipoAsync()
        {
            if (!int.TryParse(Request.Form["id"], out var id))
                return new JsonResult(new { success = false, message = "ID inválido." });

            var t = await _db.LaboratoryTypes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
            if (t == null)
                return new JsonResult(new { success = false, message = "Tipo no encontrado." });

            t.Eliminado       = false;
            t.FechaEliminado  = null;
            t.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
    }

    internal static class StringExtensions
    {
        public static string NullIfEmpty(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
