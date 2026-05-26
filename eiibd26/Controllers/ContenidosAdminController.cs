using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Encodings.Web;

namespace eiibd26.Controllers
{
    [Authorize(Roles = "Administrador")] // ✅ FIX: Cambiar "Admin" a "Administrador" (nombre real del rol)
    [Route("api/admin/contenidos")]
    [ApiController]
    public class ContenidosAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ContenidosAdminController> _logger;

        public ContenidosAdminController(ApplicationDbContext db, ILogger<ContenidosAdminController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // API endpoint para DataTables - alternativa al Page Handler
        [HttpGet("griddata")]
        public async Task<IActionResult> GetGridData(
            bool mostrarEliminados = false,
            int draw = 1,
            int start = 0,
            int length = 10,
            string searchValue = "")
        {
            try
            {
                searchValue = (searchValue ?? "").Trim();

                // Mostrar eliminados
                string mostrarElimQuery = Request.Query["mostrarEliminados"].ToString();
                if (string.IsNullOrEmpty(mostrarElimQuery))
                {
                    bool.TryParse(mostrarElimQuery, out mostrarEliminados);
                }

                // Additional filters (same as Page Handler)
                bool mostrarBorradores = false;
                bool showImages = false;
                int? idCategoriaPadre = null;
                int? idSubcategoria = null;

                if (bool.TryParse(Request.Query["mostrarBorradores"], out var borr))
                    mostrarBorradores = borr;
                if (bool.TryParse(Request.Query["showImages"], out var imgs))
                    showImages = imgs;
                if (int.TryParse(Request.Query["idCategoriaPadre"], out var catPadre))
                    idCategoriaPadre = catPadre;
                if (int.TryParse(Request.Query["idSubcategoria"], out var subCat))
                    idSubcategoria = subCat;

                // Base query
                var query = _db.Contenidos.AsNoTracking().AsQueryable();

                // Apply filters
                if (!mostrarEliminados)
                    query = query.Where(c => !c.Eliminado);

                if (!mostrarBorradores)
                    query = query.Where(c => c.EstadoPublicacion > 0);

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(c =>
                        (c.ContenidoTitulo != null && c.ContenidoTitulo.Contains(searchValue)) ||
                        (c.ContenidoTextoC != null && c.ContenidoTextoC.Contains(searchValue)) ||
                        (c.Autor != null && c.Autor.Contains(searchValue))
                    );
                }

                // Category filters
                if (idCategoriaPadre.HasValue || idSubcategoria.HasValue)
                {
                    var catQuery = _db.ContenidosCategoriasRelacion.AsNoTracking().Where(r => !r.Borrado);
                    if (idSubcategoria.HasValue)
                        catQuery = catQuery.Where(r => r.IdCategoria == idSubcategoria.Value);
                    else if (idCategoriaPadre.HasValue)
                    {
                        var childCats = _db.ContenidosCategorias.AsNoTracking()
                            .Where(c => !c.Borrado && c.CategoriaPadre == idCategoriaPadre.Value)
                            .Select(c => c.Sequence).ToList();
                        childCats.Add(idCategoriaPadre.Value);
                        catQuery = catQuery.Where(r => r.IdCategoria.HasValue && childCats.Contains(r.IdCategoria.Value));
                    }
                    var contentIdsInCat = catQuery.Select(r => r.IdContenido).Distinct();
                    query = query.Where(c => contentIdsInCat.Contains(c.Id));
                }

                // Total count
                var recordsTotal = await query.CountAsync();
                var recordsFiltered = recordsTotal;

                // Sorting by date descending
                query = query.OrderByDescending(c => c.FechaCreado);

                // Pagination
                var pageItems = await query
                    .Skip(start)
                    .Take(length)
                    .Select(c => new
                    {
                        id = c.Id,
                        contenidoTitulo = c.ContenidoTitulo,
                        descripcion = c.ContenidoTextoC,
                        tipo = c.IdTipo,
                        relevante = c.EstadoPublicacion,
                        categoria = "",
                        autor = c.Autor,
                        publicado = c.EstadoPublicacion,
                        fechaCreado = c.FechaCreado,
                        eliminado = c.Eliminado,
                        imagenUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal)
                    })
                    .ToListAsync();

                // Resolve categories (same as Page Handler)
                var contentIds = pageItems.Select(p => p.id).ToList();
                var catRels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Join(_db.ContenidosCategorias.AsNoTracking(),
                          rel => rel.IdCategoria,
                          cat => cat.Sequence,
                          (rel, cat) => new { rel.IdContenido, cat.Sequence, cat.Nombre, cat.CategoriaSlug, cat.CategoriaPadre })
                    .ToListAsync();

                var catMap = catRels.GroupBy(x => x.IdContenido).ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var chosen = g.OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1).ThenBy(x => x.Sequence).FirstOrDefault();
                        return chosen != null ? (Name: chosen.Nombre, Slug: chosen.CategoriaSlug) : (Name: (string)null, Slug: (string)null);
                    });

                // Build result with category names
                var data = pageItems.Select(p =>
                {
                    var catName = catMap.TryGetValue(p.id, out var catInfo) && !string.IsNullOrEmpty(catInfo.Name)
                        ? catInfo.Name
                        : "";

                    // SEC-014: Usar HtmlEncoder al embeber valores en HTML generado en servidor.
                    var safeId = HtmlEncoder.Default.Encode(p.id.ToString());
                    var editBtn = $"<a href='/Identity/Admin/Contenidos/Detalle?id={safeId}' class='btn btn-sm btn-outline-primary'><i class='bi bi-pencil'></i></a>";
                    var deleteForm = $"<form method='post' style='display:inline' onsubmit='return confirm(\"¿Eliminar?\");'><input type='hidden' name='id' value='{safeId}'/><button type='submit' name='handler' value='Eliminar' class='btn btn-sm btn-outline-danger'><i class='bi bi-trash'></i></button></form>";
                    var cloneForm = $"<form method='post' style='display:inline' onsubmit='return confirm(\"¿Clonar?\");'><input type='hidden' name='id' value='{safeId}'/><button type='submit' name='handler' value='Clone' class='btn btn-sm btn-outline-secondary'><i class='bi bi-files'></i></button></form>";

                    return new
                    {
                        id = p.id,
                        titulo = p.contenidoTitulo,
                        descripcion = p.descripcion,
                        tipo = p.tipo,
                        relevante = p.relevante,
                        categoria = catName,
                        autor = p.autor,
                        publicado = p.publicado,
                        fechaCreado = p.fechaCreado,
                        eliminado = p.eliminado,
                        imagenUrl = p.imagenUrl,
                        actions = editBtn + deleteForm + cloneForm
                    };
                }).ToList();

                return Ok(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando grid data via API Controller");
                return Ok(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new object[0] });
            }
        }
    }
}
