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
    // DTO simple para evitar problemas con tipos anónimos y dinámicos
    public class SintomaGridItem
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
        public int TipoSintoma { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public void OnGet() { }

        /// <summary>POST shim so DataTables can send column metadata in the body instead of the query string, avoiding the IIS 2048-char query string limit.</summary>
        public Task<IActionResult> OnPostGridDataAsync(bool mostrarEliminados = false)
            => GridDataCoreAsync(mostrarEliminados);

        public Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
            => GridDataCoreAsync(mostrarEliminados);

        private string Param(string key)
            => Request.HasFormContentType && Request.Form.ContainsKey(key)
                ? Request.Form[key].ToString()
                : Request.Query[key].ToString();

        private async Task<IActionResult> GridDataCoreAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Param("draw"), out var dVal) ? dVal : 1;
            var start = int.TryParse(Param("start"), out var sVal) ? sVal : 0;
            var length = int.TryParse(Param("length"), out var lVal) ? lVal : 10;
            var searchValue = Param("search[value]");

            // Filtrar en EF Core ANTES de Select (en SQL)
            var baseQuery = _db.sintomas
                .AsNoTracking()
                .Where(s => mostrarEliminados || !s.Eliminado);

            // Aplicar búsqueda en SQL
            if (!string.IsNullOrEmpty(searchValue))
            {
                baseQuery = baseQuery.Where(s => s.nombre.Contains(searchValue));
            }

            // Ahora proyectar al DTO
            var projectedQuery = baseQuery.Select(s => new SintomaGridItem
            {
                id = s.id,
                nombre = s.nombre ?? string.Empty,
                idPadre = s.idPadre,
                idIdioma = s.idIdioma,
                Eliminado = s.Eliminado,
                icono = s.icono ?? string.Empty,
                ValidadoIA = s.ValidadoIA,
                ValidadoHumano = s.ValidadoHumano,
                RelacionEII = s.RelacionEII,
                TipoSintoma = s.TipoSintoma
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
            var padresExtra = new List<SintomaGridItem>();
            if (padresExtraIds.Any())
            {
                padresExtra = await _db.sintomas
                    .AsNoTracking()
                    .Where(s => padresExtraIds.Contains(s.id))
                    .Select(s => new SintomaGridItem
                    {
                        id = s.id,
                        nombre = s.nombre ?? string.Empty,
                        idPadre = s.idPadre,
                        idIdioma = s.idIdioma,
                        Eliminado = s.Eliminado,
                        icono = s.icono ?? string.Empty,
                        ValidadoIA = s.ValidadoIA,
                        ValidadoHumano = s.ValidadoHumano,
                        RelacionEII = s.RelacionEII,
                        TipoSintoma = s.TipoSintoma
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
                    eliminado = padre.Eliminado,
                    validadoIA = padre.ValidadoIA,
                    validadoHumano = padre.ValidadoHumano,
                    relacionEII = padre.RelacionEII,
                    tipoSintoma = padre.TipoSintoma
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
                        eliminado = h.Eliminado,
                        validadoIA = h.ValidadoIA,
                        validadoHumano = h.ValidadoHumano,
                        relacionEII = h.RelacionEII,
                        tipoSintoma = h.TipoSintoma
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
            try
            {
                var s = await _db.sintomas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.id == id);

                if (s == null)
                {
                    return new JsonResult(new { success = false, message = "Síntoma no encontrado." }) { StatusCode = 404 };
                }

                return new JsonResult(new
                {
                    success = true,
                    id = s.id,
                    nombre = s.nombre ?? string.Empty,
                    idPadre = s.idPadre,
                    idIdioma = s.idIdioma,
                    icono = s.icono ?? string.Empty,
                    eliminado = s.Eliminado,
                    tipoSintoma = s.TipoSintoma
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OnGetGetSintomaAsync error: " + ex);
                return new JsonResult(new { success = false, message = "Error al obtener el síntoma." }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostEditarSintomaAsync()
        {
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
            if (int.TryParse(Request.Form["tipoSintoma"], out var tipoSintomaParsed))
                sintoma.TipoSintoma = Math.Clamp(tipoSintomaParsed, 0, 2);
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