using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.Glossary;
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
        /// <summary>Triage de limpieza: null = no revisado · 1 = Válido · 2 = Basura · 3 = Dudoso.</summary>
        public byte? RevisionLimpiezaEstado { get; set; }
        public string? RevisionLimpiezaMotivo { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IGlossaryService _glossary;

        public IndexModel(ApplicationDbContext db, IGlossaryService glossary)
        {
            _db = db;
            _glossary = glossary;
        }

        public void OnGet() { }

        /// <summary>POST shim so DataTables can send column metadata in the body instead of the query string, avoiding the IIS 2048-char query string limit.</summary>
        public Task<IActionResult> OnPostGridDataAsync(bool mostrarEliminados = false, string? filtroTriage = null)
            => GridDataCoreAsync(mostrarEliminados, filtroTriage);

        public Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false, string? filtroTriage = null)
            => GridDataCoreAsync(mostrarEliminados, filtroTriage);

        private string Param(string key)
            => Request.HasFormContentType && Request.Form.ContainsKey(key)
                ? Request.Form[key].ToString()
                : Request.Query[key].ToString();

        private async Task<IActionResult> GridDataCoreAsync(bool mostrarEliminados = false, string? filtroTriage = null)
        {
            var draw = int.TryParse(Param("draw"), out var dVal) ? dVal : 1;
            var start = int.TryParse(Param("start"), out var sVal) ? sVal : 0;
            var length = int.TryParse(Param("length"), out var lVal) ? lVal : 10;
            var searchValue = Param("search[value]");

            // Filtrar en EF Core ANTES de Select (en SQL)
            var baseQuery = _db.tratamientos
                .AsNoTracking()
                .Where(t => mostrarEliminados || !t.Eliminado);

            // Aplicar búsqueda en SQL
            if (!string.IsNullOrEmpty(searchValue))
            {
                baseQuery = baseQuery.Where(t => t.nombre.Contains(searchValue));
            }

            // Filtro por bucket del triage de limpieza. "dudoso" es la cola de trabajo humano.
            baseQuery = filtroTriage switch
            {
                "norevisado" => baseQuery.Where(t => t.RevisionLimpiezaEstado == null),
                "valido"     => baseQuery.Where(t => t.RevisionLimpiezaEstado == 1),
                "basura"     => baseQuery.Where(t => t.RevisionLimpiezaEstado == 2),
                "dudoso"     => baseQuery.Where(t => t.RevisionLimpiezaEstado == 3),
                _            => baseQuery
            };

            // Ahora proyectar al DTO
            var projectedQuery = baseQuery.Select(t => new TratamientoGridItem
            {
                id = t.id,
                nombre = t.nombre ?? string.Empty,
                idPadre = t.idPadre,
                idIdioma = t.idIdioma,
                Eliminado = t.Eliminado,
                icono = t.icono ?? string.Empty,
                ValidadoIA = t.ValidadoIA,
                ValidadoHumano = t.ValidadoHumano,
                RelacionEII = t.RelacionEII,
                RevisionLimpiezaEstado = t.RevisionLimpiezaEstado,
                RevisionLimpiezaMotivo = t.RevisionLimpiezaMotivo
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
                        icono = t.icono,
                        ValidadoIA = t.ValidadoIA,
                        ValidadoHumano = t.ValidadoHumano,
                        RelacionEII = t.RelacionEII
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
                    triageEstado = padre.RevisionLimpiezaEstado,
                    triageMotivo = padre.RevisionLimpiezaMotivo
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
                        triageEstado = h.RevisionLimpiezaEstado,
                        triageMotivo = h.RevisionLimpiezaMotivo
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

            // Propagar el nombre al GlossaryTerm vinculado (si existe)
            var link = await _db.GlossaryTermMedicalLinks
                .FirstOrDefaultAsync(l => l.TratamientoId == id);
            if (link != null)
            {
                var glossaryTerm = await _db.GlossaryTerms
                    .FirstOrDefaultAsync(g => g.Id == link.GlossaryTermId);
                if (glossaryTerm != null && !string.IsNullOrWhiteSpace(nombre))
                {
                    glossaryTerm.Nombre = nombre;
                    glossaryTerm.FechaActualizacion = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }

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

            // Invariante: tratamiento eliminado ⇒ su término del glosario deja de listarse.
            await _glossary.SincronizarActivoPorTratamientosAsync(new[] { id }, activo: false);

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRestaurarTratamientoAsync()
        {
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

            // Invariante (lado reversible): restaurar el tratamiento reactiva su término.
            await _glossary.SincronizarActivoPorTratamientosAsync(new[] { id }, activo: true);

            return new JsonResult(new { success = true });
        }
    }
}