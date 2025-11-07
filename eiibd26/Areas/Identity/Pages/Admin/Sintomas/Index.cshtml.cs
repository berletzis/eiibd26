using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Sintomas
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db)
        {
            System.Diagnostics.Debug.WriteLine("*** Constructor IndexModel Sintomas ejecutado ***");
            _db = db;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            var baseQuery = _db.sintomas.Where(s => mostrarEliminados || !s.Eliminado);

            // 1. Todos los síntomas que cumplan el filtro (hijos y padres)
            var filtered = string.IsNullOrEmpty(searchValue)
                ? await baseQuery.ToListAsync()
                : await baseQuery
                    .Where(s => s.nombre.Contains(searchValue))
                    .ToListAsync();

            // 2. Separa padres filtrados y los hijos filtrados
            var hijosConPadre = filtered.Where(x => x.idPadre != null).ToList();
            var padresFiltrados = filtered.Where(x => x.idPadre == null).ToList();

            // 3. IDs de padres de esos hijos que no están ya dentro del filtro
            var padresExtraIds = hijosConPadre
                .Select(x => x.idPadre.Value)
                .Distinct()
                .Where(pid => padresFiltrados.All(p => p.id != pid))
                .ToList();

            // 4. Trae los padres estrictamente necesarios para mostrar los hijos
            var padresExtra = new List<sintomas>();
            if (padresExtraIds.Any())
            {
                padresExtra = await _db.sintomas
                    .Where(s => padresExtraIds.Contains(s.id))
                    .ToListAsync();
            }

            // 5. Arma la jerarquía para la grilla:
            var resultado = new List<dynamic>();
            foreach (var padre in padresFiltrados.Concat(padresExtra).OrderBy(p => p.nombre))
            {
                resultado.Add(new
                {
                    id = padre.id,
                    nombre = padre.nombre,
                    esPadre = true,
                    idPadre = padre.idPadre,
                    idIdioma = padre.idIdioma,
                    icono = padre.icono,
                    eliminado = padre.Eliminado
                });

                var hijos = hijosConPadre
                    .Where(h => h.idPadre == padre.id)
                    .OrderBy(h => h.nombre)
                    .ToList();

                foreach (var h in hijos)
                {
                    resultado.Add(new
                    {
                        id = h.id,
                        nombre = h.nombre,
                        esPadre = false,
                        idPadre = h.idPadre,
                        idIdioma = h.idIdioma,
                        icono = h.icono,
                        eliminado = h.Eliminado
                    });
                }
            }

            var recordsTotal = resultado.Count;
            var data = resultado.Skip(start).Take(length).ToList();

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }

        public async Task<IActionResult> OnGetGetSintomaAsync(int id)
        {
            var s = await _db.sintomas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);

            if (s == null) return NotFound();

            return new JsonResult(new
            {
                id = s.id,
                nombre = s.nombre,
                idPadre = s.idPadre,
                idIdioma = s.idIdioma,
                icono = s.icono,
                eliminado = s.Eliminado
            });
        }

        public async Task<IActionResult> OnPostEditarSintomaAsync()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER EditarSintoma ==========");
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);
            var nombre = Request.Form["nombre"].ToString();
            var idPadreStr = Request.Form["idPadre"].ToString();
            int? idPadre = string.IsNullOrWhiteSpace(idPadreStr) ? (int?)null : int.Parse(idPadreStr);
            var idIdioma = int.Parse(Request.Form["idIdioma"]);
            var icono = Request.Form["icono"].ToString();
            var eliminado = Request.Form["eliminado"].ToString() == "true";

            var sintoma = await _db.sintomas.FirstOrDefaultAsync(x => x.id == id);
            if (sintoma == null)
                return new JsonResult(new { success = false, message = "Síntoma no encontrado." });

            sintoma.nombre = nombre;
            sintoma.idPadre = idPadre;
            sintoma.idIdioma = idIdioma;
            sintoma.icono = icono;
            sintoma.Eliminado = eliminado;
            sintoma.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEliminarSintomaAsync()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);

            var s = await _db.sintomas.FirstOrDefaultAsync(x => x.id == id);
            if (s == null)
                return new JsonResult(new { success = false, message = "Síntoma no encontrado." });

            // Verifica si es padre y tiene hijos
            var esPadre = s.idPadre == null;
            var tieneHijos = await _db.sintomas.AnyAsync(x => x.idPadre == id);

            if (esPadre && tieneHijos)
                return new JsonResult(new { success = false, message = "No puedes eliminar síntomas padre que tienen hijos. Elimina o reasigna primero sus hijos." });

            s.Eliminado = true;
            s.fechaEliminado = DateTime.Now.Date;
            s.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRestaurarSintomaAsync()
        {
            var ct = Request.ContentType;
            System.Diagnostics.Debug.WriteLine("Content-Type received: " + ct);
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);

            var s = await _db.sintomas.FirstOrDefaultAsync(x => x.id == id);
            if (s == null)
                return new JsonResult(new { success = false, message = "Síntoma no encontrado." });

            s.Eliminado = false;
            s.fechaEliminado = DateTime.MinValue;
            s.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}