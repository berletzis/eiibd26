using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            var mostrarBorradoresStr = Request.Query["mostrarBorradores"].ToString();
            bool mostrarBorradores = mostrarBorradoresStr == "1"
                || mostrarBorradoresStr.Equals("true", StringComparison.OrdinalIgnoreCase)
                || mostrarBorradoresStr.Equals("on", StringComparison.OrdinalIgnoreCase);

            IQueryable<Contenido> baseQuery = mostrarEliminados
                ? _db.Contenidos.IgnoreQueryFilters()
                : _db.Contenidos;

            if (!mostrarEliminados)
                baseQuery = baseQuery.Where(c => !c.Eliminado);

            if (!mostrarBorradores)
                baseQuery = baseQuery.Where(c => c.EstadoPublicacion != 0);

            if (!string.IsNullOrEmpty(searchValue))
                baseQuery = baseQuery.Where(c => c.ContenidoTitulo.Contains(searchValue));

            var recordsTotal = await baseQuery.CountAsync();

            var data = await baseQuery
                .OrderByDescending(c => c.FechaCreado)
                .Skip(start)
                .Take(length)
                .Select(c => new {
                    id = c.Id,
                    titulo = c.ContenidoTitulo,
                    descripcion = c.ContenidoTextoC,
                    tipo = c.IdTipo,
                    publicado = c.EstadoPublicacion,
                    fechaCreado = c.FechaCreado,
                    eliminado = c.Eliminado,
                    imagenUrl = string.IsNullOrWhiteSpace(c.URLImagenPrincipal)
                        ? (string)null
                        : (c.URLImagenPrincipal.StartsWith("http") || c.URLImagenPrincipal.StartsWith("/")
                            ? c.URLImagenPrincipal
                            : "/uploads/contenidos/" + c.URLImagenPrincipal)
                })
                .ToListAsync();

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }

        public async Task<IActionResult> OnGetGetContenidoAsync(int id)
        {
            var c = await _db.Contenidos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            return new JsonResult(new
            {
                id = c.Id,
                titulo = c.ContenidoTitulo,
                descripcion = c.ContenidoTextoC,
                tipo = c.IdTipo,
                publicado = c.EstadoPublicacion,
                eliminado = c.Eliminado
            });
        }

        public async Task<IActionResult> OnPostEditarContenidoAsync()
        {
            if (string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);
            var titulo = Request.Form["titulo"].ToString();
            var descripcion = Request.Form["descripcion"].ToString();
            var tipo = int.Parse(Request.Form["tipo"]);
            var publicado = int.Parse(Request.Form["publicado"]);

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

        public async Task<IActionResult> OnPostEliminarContenidoAsync()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);

            var c = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            c.Eliminado = true;
            c.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRestaurarContenidoAsync()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);

            var c = await _db.Contenidos.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id && x.Eliminado);
            if (c == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            c.Eliminado = false;
            c.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}