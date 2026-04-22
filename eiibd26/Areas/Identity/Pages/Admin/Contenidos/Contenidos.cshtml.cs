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
    public class ContenidosModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public ContenidosModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostGridData()
        {

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

            // ✅ Si mostrar eliminados, ignorar el filtro global
            IQueryable<Contenido> query = mostrarEliminados
                ? _db.Contenidos.IgnoreQueryFilters()  // ✅ Ignorar filtro global para ver eliminados
                : _db.Contenidos;

            // Leer filtros de categorías enviados por el cliente
            var categoriaPadreStr = Request.Form["categoriaPadre"].ToString();
            int? categoriaPadre = string.IsNullOrWhiteSpace(categoriaPadreStr) ? (int?)null : int.Parse(categoriaPadreStr);
            var categoriaHijoStr = Request.Form["categoriaHijo"].ToString();
            int? categoriaHijo = string.IsNullOrWhiteSpace(categoriaHijoStr) ? (int?)null : int.Parse(categoriaHijoStr);

            // Apply explicit filters: deleted and publication (drafts) are independent
            if (!mostrarEliminados)
            {
                query = query.Where(c => !c.Eliminado);
            }

            if (!mostrarBorradores)
            {
                query = query.Where(c => c.EstadoPublicacion != null && c.EstadoPublicacion != 0);
            }

            var allItems = await query
                .Where(c => string.IsNullOrEmpty(searchValue) ||
                    c.ContenidoTitulo.Contains(searchValue) ||
                    (c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(searchValue)))
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

            // Aplicar filtro por categorías (si se especificó)
            if (categoriaHijo.HasValue || categoriaPadre.HasValue)
            {
                var contentIds = allItems.Select(x => x.id).ToList();
                var relsQuery = _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.IdContenido) && r.IdCategoria != null);
                if (!mostrarEliminados)
                    relsQuery = relsQuery.Where(r => !r.Borrado);
                var rels = await relsQuery.ToListAsync();

                List<int> filterCategoryIds = new();
                if (categoriaHijo.HasValue)
                {
                    filterCategoryIds.Add(categoriaHijo.Value);
                }
                else if (categoriaPadre.HasValue)
                {
                    // obtener categorias que sean el padre o hijos directos del padre
                    var catIds = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(cc => cc.Sequence == categoriaPadre.Value || cc.CategoriaPadre == categoriaPadre.Value)
                        .Select(cc => cc.Sequence)
                        .ToListAsync();
                    filterCategoryIds.AddRange(catIds);
                }

                var allowedContentIds = rels.Where(r => r.IdCategoria.HasValue && filterCategoryIds.Contains(r.IdCategoria.Value)).Select(r => r.IdContenido).Distinct().ToHashSet();
                allItems = allItems.Where(a => allowedContentIds.Contains(a.id)).ToList();
            }

            // Obtener categoria hijo asignada (última) para cada contenido
            var contentIds2 = allItems.Select(x => x.id).ToList();
            var relsAllQuery = _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => contentIds2.Contains(r.IdContenido) && r.IdCategoria != null);
            if (!mostrarEliminados)
                relsAllQuery = relsAllQuery.Where(r => !r.Borrado);
            var relsAll = await relsAllQuery.ToListAsync();

            // Map content id -> list of category names (principal first), deduplicated
            var categoriaMap = new Dictionary<int, List<string>>();
            if (relsAll.Any())
            {
                var catIds = relsAll.Where(r => r.IdCategoria.HasValue).Select(r => r.IdCategoria.Value).Distinct().ToList();
                var catEntities = await _db.ContenidosCategorias.AsNoTracking()
                    .Where(c => catIds.Contains(c.Sequence))
                    .Select(c => new { c.Sequence, c.Nombre, c.CategoriaPadre })
                    .ToListAsync();

                var catsBySeq = catEntities.ToDictionary(c => c.Sequence, c => (Name: c.Nombre ?? string.Empty, Parent: c.CategoriaPadre));

                foreach (var gid in relsAll.GroupBy(r => r.IdContenido))
                {
                    // Order relations: principal first, then most recent
                    var ordered = gid
                        .Where(r => r.IdCategoria.HasValue)
                        .OrderByDescending(r => r.EsPrincipal == true)
                        .ThenByDescending(r => r.FechaCreacion)
                        .Select(r => r.IdCategoria.Value)
                        .ToList();

                    var names = new List<string>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var catId in ordered)
                    {
                        if (!catsBySeq.TryGetValue(catId, out var ce)) continue;
                        var display = string.IsNullOrWhiteSpace(ce.Name) ? catId.ToString() : ce.Name;
                        if (seen.Add(display))
                        {
                            // mark parent categories with <strong>
                            if (!ce.Parent.HasValue)
                                names.Add("<strong>" + System.Net.WebUtility.HtmlEncode(display) + "</strong>");
                            else
                                names.Add(System.Net.WebUtility.HtmlEncode(display));
                        }
                    }

                    if (names.Any()) categoriaMap[gid.Key] = names;
                }
            }

            // total records in DB (ignore query filters so info shows full total for "filtered from X total")
            var recordsTotal = await _db.Contenidos.IgnoreQueryFilters().CountAsync();
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

                // attach categoria names into each data row (comma-separated)
            var finalData = data.Select(d => new {
                d.id,
                d.titulo,
                d.descripcion,
                d.tipo,
                d.publicado,
                d.fechaCreado,
                d.eliminado,
                d.imagenUrl,
                categorias = categoriaMap.ContainsKey(d.id) ? string.Join(", ", categoriaMap[d.id]) : (string)null
            }).ToList();

            // Safety: ensure drafts are not returned unless mostrarBorradores is true
            if (!mostrarBorradores)
            {
                finalData = finalData.Where(d => d.publicado != 0).ToList();
            }

            // recordsFiltered must be the total count after applying filters (before paging)
            var recordsFiltered = allItems.Count;

            return new JsonResult(new
            {
                draw,
                recordsTotal,
                recordsFiltered,
                data = finalData
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

        // POST: Fill missing slugs for published, not-deleted contents
        public async Task<IActionResult> OnPostFillMissingSlugs()
        {
            try
            {
                var candidates = await _db.Contenidos
                    .Where(c => !c.Eliminado && c.EstadoPublicacion != null && c.EstadoPublicacion != 0 && (c.ContenidoTituloSlug == null || c.ContenidoTituloSlug == "") && !string.IsNullOrWhiteSpace(c.ContenidoTitulo))
                    .ToListAsync();

                int modified = 0;
                foreach (var c in candidates)
                {
                    var slug = SlugifyLocal(c.ContenidoTitulo);
                    if (string.IsNullOrWhiteSpace(slug)) continue;

                    // Ensure uniqueness among non-deleted contents
                    var exists = await _db.Contenidos.AsNoTracking().AnyAsync(x => x.ContenidoTituloSlug == slug && !x.Eliminado);
                    if (exists)
                    {
                        slug = slug + "-" + c.Id;
                    }

                    c.ContenidoTituloSlug = slug;
                    c.FechaModificado = DateTime.UtcNow;
                    modified++;
                }

                if (modified > 0) await _db.SaveChangesAsync();

                return new JsonResult(new { modified });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private static string SlugifyLocal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var s = value.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            var chars = s.Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            var cleaned = new string(chars);
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^a-z0-9\s-]", "").Trim();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", "-");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "-+", "-");
            return cleaned;
        }

        // Categories list for parent select (hierarchical label)
        public async Task<IActionResult> OnGetCategoriesList()
        {
            var cats = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado)
                .ToListAsync();

            var dict = cats.ToDictionary(c => c.Sequence, c => c);
            var items = cats
                .Select(c => new { sequence = c.Sequence, label = BuildLabel(c, dict) })
                .OrderBy(x => x.label)
                .ToList();

            return new JsonResult(items);
        }

        // Children list for a given parent
        public async Task<IActionResult> OnGetChildrenList(int parent)
        {
            var children = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => c.CategoriaPadre == parent && !c.Borrado)
                .OrderBy(c => c.Nombre)
                .Select(c => new { sequence = c.Sequence, nombre = c.Nombre })
                .ToListAsync();

            return new JsonResult(children);
        }

        private static string BuildLabel(ContenidoCategoria node, Dictionary<int, ContenidoCategoria> dict)
        {
            var parts = new List<string>();
            var cur = node;
            var safety = 0;
            while (cur != null && safety++ < 50)
            {
                if (!string.IsNullOrWhiteSpace(cur.Nombre)) parts.Add(cur.Nombre.Trim());
                if (!cur.CategoriaPadre.HasValue) break;
                if (!dict.TryGetValue(cur.CategoriaPadre.Value, out cur)) break;
            }
            parts.Reverse();
            return string.Join(" > ", parts);
        }

        public async Task<IActionResult> OnPostEliminarContenido()
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

        public async Task<IActionResult> OnPostRestaurarContenido()
        {
            if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                return BadRequest(new { success = false, message = "ID inválido" });

            var id = int.Parse(Request.Form["id"]);

            var c = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null)
                return new JsonResult(new { success = false, message = "Contenido no encontrado." });

            c.Eliminado = false;
            c.FechaModificado = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }
    }
}