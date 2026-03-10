using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Tratamientos
{
    // DTO simple para evitar problemas con tipos anónimos y dinámicos
    public class TratamientoGridItem
    {
        public int id { get; set; }
        public string? nombre { get; set; }
        public int? idPadre { get; set; }
        public int? idIdioma { get; set; }
        public bool Eliminado { get; set; }
        public string? icono { get; set; }
        public bool ValidadoIA { get; set; }
        public bool ValidadoHumano { get; set; }
        public bool RelacionEII { get; set; }
    }

    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db)
        {
            System.Diagnostics.Debug.WriteLine("*** Constructor IndexModel Tratamientos ejecutado ***");
            _db = db;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            // Filtrar en EF Core ANTES de Select (en SQL)
            var baseQuery = _db.tratamientos
                .AsNoTracking()
                .Where(t => mostrarEliminados || !t.Eliminado);

            // Aplicar búsqueda en SQL
            if (!string.IsNullOrEmpty(searchValue))
            {
                baseQuery = baseQuery.Where(t => t.nombre.Contains(searchValue));
            }

            // Ahora proyectar al DTO
            var projectedQuery = baseQuery.Select(t => new TratamientoGridItem
            {
                id = t.id,
                nombre = t.nombre ?? string.Empty,
                idPadre = t.idPadre,
                idIdioma = t.idIdioma,
                Eliminado = t.Eliminado,
                icono = t.icono ?? string.Empty
            });

            // 1. Ejecutar la query y traer datos a memoria
            var filtered = await projectedQuery.ToListAsync();

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
            var padresExtra = new List<TratamientoGridItem>();
            if (padresExtraIds.Any())
            {
                padresExtra = await _db.tratamientos
                    .AsNoTracking()
                    .Where(t => padresExtraIds.Contains(t.id))
                    .Select(t => new TratamientoGridItem
                    {
                        id = t.id,
                        nombre = t.nombre,
                        idPadre = t.idPadre,
                        idIdioma = t.idIdioma,
                        Eliminado = t.Eliminado,
                        icono = t.icono
                    })
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

        public async Task<IActionResult> OnGetGetTratamientoAsync(int id)
        {
            var t = await _db.tratamientos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == id);

            if (t == null) return NotFound();

            return new JsonResult(new
            {
                id = t.id,
                nombre = t.nombre,
                idPadre = t.idPadre,
                idIdioma = t.idIdioma,
                icono = t.icono,
                eliminado = t.Eliminado
            });
        }

        public async Task<IActionResult> OnPostEditarTratamientoAsync()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER EditarTratamiento ==========");
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

            var tratamiento = await _db.tratamientos.FirstOrDefaultAsync(x => x.id == id);
            if (tratamiento == null)
                return new JsonResult(new { success = false, message = "Tratamiento no encontrado." });

            tratamiento.nombre = nombre;
            tratamiento.idPadre = idPadre;
            tratamiento.idIdioma = idIdioma;
            tratamiento.icono = icono;
            tratamiento.Eliminado = eliminado;
            tratamiento.fechaModificado = DateTime.Now;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEliminarTratamientoAsync()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);

            var t = await _db.tratamientos.FirstOrDefaultAsync(x => x.id == id);
            if (t == null)
                return new JsonResult(new { success = false, message = "Tratamiento no encontrado." });

            // Verifica si es padre y tiene hijos
            var esPadre = t.idPadre == null;
            var tieneHijos = await _db.tratamientos.AnyAsync(x => x.idPadre == id);

            if (esPadre && tieneHijos)
                return new JsonResult(new { success = false, message = "No puedes eliminar tratamientos padre que tienen hijos. Elimina o reasigna primero sus hijos." });

            t.Eliminado = true;
            t.fechaEliminado = DateTime.Now.Date;
            t.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRestaurarTratamientoAsync()
        {
            var ct = Request.ContentType;
            System.Diagnostics.Debug.WriteLine("Content-Type received: " + ct);
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });
            var id = int.Parse(Request.Form["id"]);

            var t = await _db.tratamientos.FirstOrDefaultAsync(x => x.id == id);
            if (t == null)
                return new JsonResult(new { success = false, message = "Tratamiento no encontrado." });

            t.Eliminado = false;
            t.fechaEliminado = DateTime.MinValue;
            t.fechaModificado = DateTime.Now;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}