// Areas/Identity/Pages/Admin/Contenidos/ContenidosCategorias.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    public class ContenidosCategoriasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ContenidosCategoriasModel(ApplicationDbContext db) => _db = db;

        // validation limits aligned with DB schema
        public int MaxNombreLength => 400;   // varchar(400)
        public int MaxClaveLength => 10;     // varchar(10) <-- was causing truncation
        public int MaxSlugLength => 1000;    // DB allows large (varchar(max)), keep reasonable limit
        public int MaxImagenLength => 4000;  // DB allows large (varchar(max)), keep reasonable limit

        // Parent select list used in modals (Label contains hierarchical path)
        public List<ContenidoCategoriaDto> AllCategoriesSelect { get; set; } = new();

        public class ContenidoCategoriaDto
        {
            public int Sequence { get; set; }
            public int? CategoriaPadre { get; set; }
            public string Nombre { get; set; } = "";
            public string Label { get; set; } = ""; // hierarchical label for select
        }

        public async Task OnGetAsync()
        {
            // load parent select for modals
            // Antes: LoadSelectList().GetAwaiter().GetResult() — sync-over-async, bloquea un hilo
            // del pool en una I/O remota. El resto de los llamadores ya usaban await.
            await LoadSelectList();
        }

        private async Task LoadSelectList()
        {
            var cats = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado)
                .ToListAsync();

            var dict = cats.ToDictionary(c => c.Sequence, c => c);

            var list = new List<ContenidoCategoriaDto>();
            foreach (var c in cats.OrderBy(c => c.Nombre))
            {
                var label = BuildHierarchyLabel(c, dict);
                list.Add(new ContenidoCategoriaDto
                {
                    Sequence = c.Sequence,
                    CategoriaPadre = c.CategoriaPadre,
                    Nombre = c.Nombre ?? "",
                    Label = label
                });
            }

            AllCategoriesSelect = list.OrderBy(l => l.Label).ToList();
        }

        private static string BuildHierarchyLabel(ContenidoCategoria node, Dictionary<int, ContenidoCategoria> dict)
        {
            var parts = new List<string>();
            var cur = node;
            var safety = 0;
            while (cur != null && safety++ < 50)
            {
                if (!string.IsNullOrWhiteSpace(cur.Nombre))
                    parts.Add(cur.Nombre.Trim());
                if (!cur.CategoriaPadre.HasValue) break;
                if (!dict.TryGetValue(cur.CategoriaPadre.Value, out cur)) break;
            }
            parts.Reverse();
            return string.Join(" > ", parts);
        }

        // Grid endpoint for DataTables - returns camelCase
        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            var query = _db.ContenidosCategorias.AsNoTracking()
                .Where(c => mostrarEliminados || !c.Borrado);

            if (!string.IsNullOrWhiteSpace(searchValue))
                query = query.Where(c => c.Nombre != null && c.Nombre.Contains(searchValue));

            var list = await query
                .Select(c => new
                {
                    c.Sequence,
                    c.Nombre,
                    c.CategoriaPadre,
                    c.Clave,
                    c.CategoriaSlug,
                    c.Imagen,
                    c.Relevante,
                    FechaCreacion = c.FechaCreacion,
                    c.Borrado
                })
                .ToListAsync();

            var resultOrdered = new List<object>();
            var padres = list.Where(x => x.CategoriaPadre == null).OrderBy(x => x.Nombre).ToList();
            foreach (var padre in padres)
            {
                resultOrdered.Add(new
                {
                    sequence = padre.Sequence,
                    nombre = padre.Nombre,
                    categoriaPadre = (int?)null,
                    padreNombre = (string)null,
                    clave = padre.Clave,
                    categoriaSlug = padre.CategoriaSlug,
                    relevante = padre.Relevante,
                    fechaCreacion = padre.FechaCreacion.ToString("yyyy-MM-dd"),
                    borrado = padre.Borrado
                });

                var hijos = list.Where(h => h.CategoriaPadre == padre.Sequence).OrderBy(h => h.Nombre);
                foreach (var h in hijos)
                {
                    resultOrdered.Add(new
                    {
                        sequence = h.Sequence,
                        nombre = h.Nombre,
                        categoriaPadre = h.CategoriaPadre,
                        padreNombre = padre.Nombre,
                        clave = h.Clave,
                        categoriaSlug = h.CategoriaSlug,
                        relevante = h.Relevante,
                        fechaCreacion = h.FechaCreacion.ToString("yyyy-MM-dd"),
                        borrado = h.Borrado
                    });
                }
            }

            var orphans = list.Where(x => x.CategoriaPadre != null && !padres.Any(p => p.Sequence == x.CategoriaPadre)).OrderBy(x => x.Nombre);
            foreach (var o in orphans)
            {
                resultOrdered.Add(new
                {
                    sequence = o.Sequence,
                    nombre = o.Nombre,
                    categoriaPadre = o.CategoriaPadre,
                    padreNombre = (string)null,
                    clave = o.Clave,
                    categoriaSlug = o.CategoriaSlug,
                    relevante = o.Relevante,
                    fechaCreacion = o.FechaCreacion.ToString("yyyy-MM-dd"),
                    borrado = o.Borrado
                });
            }

            var recordsTotal = resultOrdered.Count;
            var pageData = resultOrdered.Skip(start).Take(length).ToList();

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data = pageData
            });
        }

        // get single category for edit modal (camelCase)
        public async Task<IActionResult> OnGetGetCategoriaAsync(int sequence)
        {
            var c = await _db.ContenidosCategorias
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Sequence == sequence);

            if (c == null) return NotFound();

            return new JsonResult(new
            {
                sequence = c.Sequence,
                nombre = c.Nombre ?? "",
                categoriaPadre = c.CategoriaPadre,
                clave = c.Clave ?? "",
                categoriaSlug = c.CategoriaSlug ?? "",
                imagen = c.Imagen ?? "",
                relevante = c.Relevante ?? false,
                borrado = c.Borrado
            });
        }

        // parents list endpoint (camelCase)
        public async Task<IActionResult> OnGetParentsListAsync()
        {
            var cats = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado)
                .ToListAsync();

            var dict = cats.ToDictionary(c => c.Sequence, c => c);
            var items = cats
                .OrderBy(c => c.Nombre)
                .Select(c => new
                {
                    sequence = c.Sequence,
                    label = BuildHierarchyLabel(c, dict)
                })
                .OrderBy(x => x.label)
                .ToList();

            return new JsonResult(items);
        }

        // create handler with validation and robust error handling
        public async Task<IActionResult> OnPostCrearCategoriaAsync()
        {
            try
            {
                var nombre = (Request.Form["Nombre"].ToString() ?? "").Trim();
                var padreStr = Request.Form["CategoriaPadre"].ToString();
                int? padre = string.IsNullOrWhiteSpace(padreStr) ? (int?)null : int.Parse(padreStr);
                var clave = (Request.Form["Clave"].ToString() ?? "").Trim();
                var slug = (Request.Form["CategoriaSlug"].ToString() ?? "").Trim();
                var imagen = (Request.Form["Imagen"].ToString() ?? "").Trim();
                var relevante = Request.Form["Relevante"].ToString().ToLower() == "on" || Request.Form["Relevante"].ToString().ToLower() == "true";

                // validations
                if (string.IsNullOrWhiteSpace(nombre))
                    return new JsonResult(new { success = false, message = "El nombre es obligatorio." });
                if (nombre.Length > MaxNombreLength)
                    return new JsonResult(new { success = false, message = $"El nombre supera los {MaxNombreLength} caracteres." });
                if (!string.IsNullOrWhiteSpace(clave) && clave.Length > MaxClaveLength)
                    return new JsonResult(new { success = false, message = $"La clave supera los {MaxClaveLength} caracteres." });
                if (!string.IsNullOrWhiteSpace(slug) && slug.Length > MaxSlugLength)
                    return new JsonResult(new { success = false, message = $"El slug supera los {MaxSlugLength} caracteres." });
                if (!string.IsNullOrWhiteSpace(imagen) && imagen.Length > MaxImagenLength)
                    return new JsonResult(new { success = false, message = $"La URL de la imagen supera los {MaxImagenLength} caracteres." });

                // unique slug check
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    var exists = await _db.ContenidosCategorias.AnyAsync(c => c.CategoriaSlug != null && c.CategoriaSlug.ToLower() == slug.ToLower());
                    if (exists)
                        return new JsonResult(new { success = false, message = "El slug ya est� en uso. Elija otro valor." });
                }

                var now = DateTime.UtcNow;
                var user = GetCurrentUserGuid();

                var entity = new ContenidoCategoria
                {
                    Nombre = nombre,
                    CategoriaPadre = padre,
                    Clave = string.IsNullOrWhiteSpace(clave) ? null : clave,
                    CategoriaSlug = string.IsNullOrWhiteSpace(slug) ? null : slug,
                    Imagen = string.IsNullOrWhiteSpace(imagen) ? null : imagen,
                    Relevante = relevante,
                    FechaCreacion = now,
                    FechaModificacion = now,
                    UsuarioCreacion = user,
                    UsuarioModificacion = user,
                    Borrado = false
                };

                _db.ContenidosCategorias.Add(entity);
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                    return new JsonResult(new { success = false, message = "Error guardando en BD: " + inner });
                }

                await LoadSelectList();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // edit handler with validation and robust error handling
        public async Task<IActionResult> OnPostEditarCategoriaAsync()
        {
            if (string.IsNullOrWhiteSpace(Request.Form["Sequence"]))
                return BadRequest(new { success = false, message = "Clave inv�lida" });

            var seq = int.Parse(Request.Form["Sequence"]);
            var nombre = (Request.Form["Nombre"].ToString() ?? "").Trim();
            var padreStr = Request.Form["CategoriaPadre"].ToString();
            int? padre = string.IsNullOrWhiteSpace(padreStr) ? (int?)null : int.Parse(padreStr);
            var clave = (Request.Form["Clave"].ToString() ?? "").Trim();
            var slug = (Request.Form["CategoriaSlug"].ToString() ?? "").Trim();
            var imagen = (Request.Form["Imagen"].ToString() ?? "").Trim();
            var relevante = Request.Form["Relevante"].ToString().ToLower() == "on" || Request.Form["Relevante"].ToString().ToLower() == "true";
            var borrado = Request.Form["Borrado"].ToString() == "true";

            var ent = await _db.ContenidosCategorias.FirstOrDefaultAsync(x => x.Sequence == seq);
            if (ent == null) return new JsonResult(new { success = false, message = "Categor�a no encontrada." });

            // Prevent self-parenting
            if (padre.HasValue && padre.Value == ent.Sequence)
                return new JsonResult(new { success = false, message = "Una categor�a no puede ser padre de s� misma." });

            // validations
            if (string.IsNullOrWhiteSpace(nombre))
                return new JsonResult(new { success = false, message = "El nombre es obligatorio." });
            if (nombre.Length > MaxNombreLength)
                return new JsonResult(new { success = false, message = $"El nombre supera los {MaxNombreLength} caracteres." });
            if (!string.IsNullOrWhiteSpace(clave) && clave.Length > MaxClaveLength)
                return new JsonResult(new { success = false, message = $"La clave supera los {MaxClaveLength} caracteres." });
            if (!string.IsNullOrWhiteSpace(slug) && slug.Length > MaxSlugLength)
                return new JsonResult(new { success = false, message = $"El slug supera los {MaxSlugLength} caracteres." });
            if (!string.IsNullOrWhiteSpace(imagen) && imagen.Length > MaxImagenLength)
                return new JsonResult(new { success = false, message = $"La URL de la imagen supera los {MaxImagenLength} caracteres." });

            // unique slug check
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var exists = await _db.ContenidosCategorias.AnyAsync(c => c.Sequence != seq && c.CategoriaSlug != null && c.CategoriaSlug.ToLower() == slug.ToLower());
                if (exists)
                    return new JsonResult(new { success = false, message = "El slug ya est� en uso por otra categor�a." });
            }

            ent.Nombre = nombre ?? ent.Nombre;
            ent.CategoriaPadre = padre;
            ent.Clave = string.IsNullOrWhiteSpace(clave) ? null : clave;
            ent.CategoriaSlug = string.IsNullOrWhiteSpace(slug) ? null : slug;
            ent.Imagen = string.IsNullOrWhiteSpace(imagen) ? null : imagen;
            ent.Relevante = relevante;
            ent.Borrado = borrado;
            ent.FechaModificacion = DateTime.UtcNow;
            ent.UsuarioModificacion = GetCurrentUserGuid();

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return new JsonResult(new { success = false, message = "Error guardando en BD: " + inner });
            }

            await LoadSelectList();
            return new JsonResult(new { success = true });
        }

        // soft-delete
        public async Task<IActionResult> OnPostEliminarCategoriaAsync(int sequence)
        {
            var ent = await _db.ContenidosCategorias.FirstOrDefaultAsync(x => x.Sequence == sequence && !x.Borrado);
            if (ent == null) return new JsonResult(new { success = false, message = "Categor�a no encontrada o ya eliminada." });

            var hasChildren = await _db.ContenidosCategorias.AnyAsync(x => x.CategoriaPadre == ent.Sequence && !x.Borrado);
            if (hasChildren) return new JsonResult(new { success = false, message = "No puede eliminar categor�a que tiene hijos." });

            ent.Borrado = true;
            ent.FechaModificacion = DateTime.UtcNow;
            ent.UsuarioModificacion = GetCurrentUserGuid();
            await _db.SaveChangesAsync();

            await LoadSelectList();
            return new JsonResult(new { success = true });
        }

        // restore
        public async Task<IActionResult> OnPostRestaurarCategoriaAsync(int sequence)
        {
            var ent = await _db.ContenidosCategorias.FirstOrDefaultAsync(x => x.Sequence == sequence && x.Borrado);
            if (ent == null) return new JsonResult(new { success = false, message = "Categor�a no encontrada o no est� eliminada." });

            ent.Borrado = false;
            ent.FechaModificacion = DateTime.UtcNow;
            ent.UsuarioModificacion = GetCurrentUserGuid();
            await _db.SaveChangesAsync();

            await LoadSelectList();
            return new JsonResult(new { success = true });
        }

        private Guid GetCurrentUserGuid()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(idStr, out var g) ? g : Guid.Empty;
        }
    }
}