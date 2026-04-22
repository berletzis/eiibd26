using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using System;

namespace eiibd26.Areas.Identity.Pages.Admin.Condiciones
{
    //[Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {

        private readonly ApplicationDbContext _db;
        //public IndexModel(ApplicationDbContext db) { _db = db; }
        public IndexModel(ApplicationDbContext db)
        {
            System.Diagnostics.Debug.WriteLine("*** Constructor IndexModel ejecutado ***");
            _db = db;
        }

        public void OnGet() { }


        
        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            var allItems = await _db.condiciones
                .Where(c => mostrarEliminados || !c.Eliminado)
                .Where(c => string.IsNullOrEmpty(searchValue) || c.nombre.Contains(searchValue))
                .Select(c => new {
                    id = c.id,
                    nombre = c.nombre,
                    esPadre = c.idPadre == null,
                    idPadre = c.idPadre,
                    idIdioma = c.idIdioma,
                    icono = c.icono,
                    eliminado = c.Eliminado
                })
                .ToListAsync();

            var resultado = new List<dynamic>();
            var padres = allItems.Where(c => c.esPadre).OrderBy(c => c.nombre).ToList();
            foreach (var padre in padres)
            {
                resultado.Add(padre);
                var hijos = allItems.Where(h => h.idPadre == padre.id).OrderBy(h => h.nombre);
                resultado.AddRange(hijos);
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

        
        public async Task<IActionResult> OnGetGetCondicionAsync(int id)
        {
            var c = await _db.condiciones
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);

            if (c == null) return NotFound();

            return new JsonResult(new
            {
                id = c.id,
                nombre = c.nombre,
                idPadre = c.idPadre,
                idIdioma = c.idIdioma,
                icono = c.icono,
                eliminado = c.Eliminado
            });
        }

        public async Task<IActionResult> OnPostEditarCondicionAsync()
        {

            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER EditarCondicion ==========");
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

            var condicion = await _db.condiciones.FirstOrDefaultAsync(x => x.id == id);

            if (condicion == null)
                return new JsonResult(new { success = false, message = "Condición no encontrada." });

            condicion.nombre = nombre;
            condicion.idPadre = idPadre;
            condicion.idIdioma = idIdioma;
            condicion.icono = icono;
            condicion.Eliminado = eliminado;
            condicion.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        
        public async Task<IActionResult> OnPostEliminarCondicionAsync()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);

            var c = await _db.condiciones.FirstOrDefaultAsync(x => x.id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Condición no encontrada." });

            var esPadre = c.idPadre == null;
            if (esPadre)
                return new JsonResult(new { success = false, message = "No puedes eliminar condiciones padre." });
            var tieneHijos = await _db.condiciones.AnyAsync(x => x.idPadre == id);
            if (tieneHijos)
                return new JsonResult(new { success = false, message = "No puedes eliminar condiciones que son padre de otras." });

            c.Eliminado = true;
            c.fechaEliminado = DateTime.Now.Date;
            c.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        
        public async Task<IActionResult> OnPostRestaurarCondicionAsync()
        {
            var ct = Request.ContentType;
            System.Diagnostics.Debug.WriteLine("Content-Type received: " + ct);
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);



            var c = await _db.condiciones.FirstOrDefaultAsync(x => x.id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Condición no encontrada." });

            c.Eliminado = false;
            c.fechaEliminado = DateTime.MinValue; // Valor por defecto para campos NOT NULL
            c.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}