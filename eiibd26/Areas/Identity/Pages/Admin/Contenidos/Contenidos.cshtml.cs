using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    //[Authorize(Roles = "Administrador")]
    [IgnoreAntiforgeryToken]
    public class ContenidosModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ContenidosModel(ApplicationDbContext db)
        {
            System.Diagnostics.Debug.WriteLine("*** Constructor ContenidosModel ejecutado ***");
            _db = db;
        }

        public void OnGet()
        {
            System.Diagnostics.Debug.WriteLine("*** OnGet() ContenidosModel ejecutado ***");
        }

        public async Task<IActionResult> OnPostGridData()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER GridData (POST) ==========");

            var draw = int.TryParse(Request.Form["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Form["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Form["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Form["search[value]"].ToString();

            // ✅ Leer switches desde Request.Form
            var mostrarEliminadosStr = Request.Form["mostrarEliminados"].ToString();
            bool mostrarEliminados = mostrarEliminadosStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                || mostrarEliminadosStr == "1"
                || mostrarEliminadosStr.Equals("on", StringComparison.OrdinalIgnoreCase);

            var mostrarBorradoresStr = Request.Form["mostrarBorradores"].ToString();
            bool mostrarBorradores = mostrarBorradoresStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                || mostrarBorradoresStr == "1"
                || mostrarBorradoresStr.Equals("on", StringComparison.OrdinalIgnoreCase);

            System.Diagnostics.Debug.WriteLine($"[GridData POST] mostrarEliminados={mostrarEliminados}, mostrarBorradores={mostrarBorradores}");

            // ✅ Si mostrar eliminados, ignorar el filtro global
            IQueryable<Contenido> query = mostrarEliminados
                ? _db.Contenidos.IgnoreQueryFilters()  // ✅ Ignorar filtro global para ver eliminados
                : _db.Contenidos;

            var allItems = await query
                .Where(c => mostrarEliminados || !c.Eliminado)  // Filtro adicional si no se muestran eliminados
                .Where(c => string.IsNullOrEmpty(searchValue) ||
                    c.ContenidoTitulo.Contains(searchValue) ||
                    (c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(searchValue)))
                .Where(c => mostrarBorradores || (c.EstadoPublicacion != null && c.EstadoPublicacion != 0))
                .Select(c => new {
                    id = c.Id,
                    titulo = c.ContenidoTitulo ?? "",
                    descripcion = c.ContenidoTextoC ?? "",
                    tipo = c.IdTipo ?? 0,
                    publicado = c.EstadoPublicacion ?? 0,
                    fechaCreado = c.FechaCreado,
                    eliminado = c.Eliminado,
                    imagenUrlRaw = c.URLImagenPrincipal
                })
                .OrderByDescending(c => c.fechaCreado)
                .ToListAsync();

            var recordsTotal = allItems.Count;
            var data = allItems
                .Skip(start)
                .Take(length)
                .Select(c => new {
                    c.id,
                    c.titulo,
                    c.descripcion,
                    c.tipo,
                    c.publicado,
                    c.fechaCreado,
                    c.eliminado,
                    imagenUrl = !string.IsNullOrWhiteSpace(c.imagenUrlRaw)
                        ? (c.imagenUrlRaw.StartsWith("http") || c.imagenUrlRaw.StartsWith("/")
                            ? c.imagenUrlRaw
                            : $"/uploads/contenidos/{c.imagenUrlRaw}")
                        : (string)null
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[GridData POST] Registros devueltos: {data.Count} (Eliminados: {data.Count(d => d.eliminado)})");

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }

        public async Task<IActionResult> OnGetGetContenido(int id)
        {
            var c = await _db.Contenidos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            return new JsonResult(new
            {
                id = c.Id,
                titulo = c.ContenidoTitulo ?? "",
                descripcion = c.ContenidoTextoC ?? "",
                tipo = c.IdTipo ?? 0,
                publicado = c.EstadoPublicacion ?? 0,
                eliminado = c.Eliminado
            });
        }

        public async Task<IActionResult> OnPostEditarContenido()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER EditarContenido ==========");
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);
            var titulo = Request.Form["titulo"].ToString();
            var descripcion = Request.Form["descripcion"].ToString();

            if (!int.TryParse(Request.Form["tipo"], out var tipo))
                tipo = 1;

            if (!int.TryParse(Request.Form["publicado"], out var publicado))
                publicado = 0;

            var contenido = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);

            if (contenido == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            contenido.ContenidoTitulo = titulo;
            contenido.ContenidoTextoC = descripcion;
            contenido.IdTipo = tipo;
            contenido.EstadoPublicacion = publicado;
            contenido.FechaModificado = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostEliminarContenido()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER EliminarContenido ==========");

            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);

            var c = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            c.Eliminado = true;
            c.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Contenido {id} eliminado correctamente");

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRestaurarContenido()
        {
            System.Diagnostics.Debug.WriteLine("========= ENTRA AL HANDLER RestaurarContenido ==========");

            var ct = Request.ContentType;
            System.Diagnostics.Debug.WriteLine("Content-Type received: " + ct);
            var formDebug = string.Join(", ", Request.Form.Select(f => $"{f.Key}={f.Value}"));
            System.Diagnostics.Debug.WriteLine("Request.Form: " + formDebug);

            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);

            var c = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            c.Eliminado = false;
            c.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            System.Diagnostics.Debug.WriteLine($"✅ Contenido {id} restaurado correctamente");

            return new JsonResult(new { success = true });
        }
    }
}