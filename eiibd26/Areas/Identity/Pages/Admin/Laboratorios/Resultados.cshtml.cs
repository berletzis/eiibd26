using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Laboratorios
{
    [Authorize(Roles = "Administrador")]
    public class ResultadosModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ResultadosModel(ApplicationDbContext db) => _db = db;

        public void OnGet() { }

        public async Task<IActionResult> OnGetGridDataAsync()
        {
            var draw   = int.TryParse(Request.Query["draw"],   out var d) ? d : 1;
            var start  = int.TryParse(Request.Query["start"],  out var s) ? s : 0;
            var length = int.TryParse(Request.Query["length"], out var l) ? l : 10;
            var search = Request.Query["search[value]"].ToString();

            var q = _db.PatientLaboratoryResults
                .IgnoreQueryFilters()
                .Include(r => r.LaboratoryType).ThenInclude(t => t.Parent)
                .Include(r => r.Patient)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r => r.Patient.Email.Contains(search)
                              || r.ResultValue.Contains(search)
                              || r.LaboratoryType.Name.Contains(search));

            var total = await q.CountAsync();

            var data = await q
                .OrderByDescending(r => r.FechaCreado)
                .Skip(start).Take(length)
                .Select(r => new
                {
                    r.Id,
                    Email        = r.Patient.Email,
                    TipoPadre    = r.LaboratoryType.Parent != null ? r.LaboratoryType.Parent.Name : r.LaboratoryType.Name,
                    TipoNombre   = r.LaboratoryType.Name,
                    r.ResultValue,
                    r.ResultUnit,
                    r.Notes,
                    ResultDate   = r.ResultDate.HasValue ? r.ResultDate.Value.ToString("dd/MM/yyyy") : "—",
                    FechaCreado  = r.FechaCreado.ToString("dd/MM/yyyy HH:mm"),
                    r.Eliminado
                })
                .ToListAsync();

            return new JsonResult(new { draw, recordsTotal = total, recordsFiltered = total, data });
        }
    }
}
