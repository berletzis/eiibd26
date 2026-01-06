using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Lista de categorías padre para el combo principal
        public List<(int seq, string name)> ParentCategories { get; set; } = new();
        // Lista completa (para JS): seq, parent, name
        public List<(int seq, int? parent, string name)> CategoriesFlat { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var rawAll = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => c.Borrado == false)
                    .Select(c => new { c.Sequence, c.CategoriaPadre, Nombre = c.Nombre ?? "" })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                CategoriesFlat = rawAll
                    .Select(c => (c.Sequence, c.CategoriaPadre, c.Nombre))
                    .ToList();

                ParentCategories = rawAll
                    .Where(c => c.CategoriaPadre == null)
                    .Select(c => (c.Sequence, c.Nombre))
                    .OrderBy(c => c.Nombre)
                    .ToList();

                _logger.LogDebug("OnGetAsync: Padres={Padres} TotalCategorias={Total}",
                    ParentCategories.Count, CategoriesFlat.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cargando categorías (padres y flat).");
                ParentCategories = new List<(int, string)>();
                CategoriesFlat = new List<(int, int?, string)>();
            }
        }

        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            try
            {
                var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
                var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
                var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
                var searchValue = (Request.Query["search[value]"].ToString() ?? "").Trim();

                // Mostrar eliminados (existing)
                string mostrarElimQuery = Request.Query["mostrarEliminados"].ToString();
                if (string.IsNullOrEmpty(mostrarElimQuery))
                {
                    foreach (var k in Request.Query.Keys)
                    {
                        if (k.IndexOf("mostrar", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            k.IndexOf("elimin", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            mostrarElimQuery = Request.Query[k].ToString();
                            break;
                        }
                    }
                }
                bool mostrarElimFlag = mostrarElimQuery == "1"
                    || mostrarElimQuery.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || mostrarElimQuery.Equals("on", StringComparison.OrdinalIgnoreCase);

                // Mostrar borradores (nuevo switch)
                string mostrarDraftsQuery = Request.Query["mostrarBorradores"].ToString();
                if (string.IsNullOrEmpty(mostrarDraftsQuery))
                {
                    foreach (var k in Request.Query.Keys)
                    {
                        if (k.IndexOf("mostrar", StringComparison.OrdinalIgnoreCase) >= 0 &&
                            (k.IndexOf("borrador", StringComparison.OrdinalIgnoreCase) >= 0 || k.IndexOf("draft", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            mostrarDraftsQuery = Request.Query[k].ToString();
                            break;
                        }
                    }
                }
                bool mostrarDraftsFlag = mostrarDraftsQuery == "1"
                    || mostrarDraftsQuery.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || mostrarDraftsQuery.Equals("on", StringComparison.OrdinalIgnoreCase);

                // Filtros de categoría
                int? idCategoriaPadre = null;
                int? idSubcategoria = null;
                var rawParent = Request.Query["idCategoriaPadre"].ToString();
                var rawSub = Request.Query["idSubcategoria"].ToString();

                if (!string.IsNullOrWhiteSpace(rawParent) && int.TryParse(rawParent, out var parsedParent) && parsedParent > 0)
                    idCategoriaPadre = parsedParent;
                if (!string.IsNullOrWhiteSpace(rawSub) && int.TryParse(rawSub, out var parsedSub) && parsedSub > 0)
                    idSubcategoria = parsedSub;

                IQueryable<eiibd26.Models.Contenido> baseQuery = mostrarElimFlag
                    ? _db.Contenidos.IgnoreQueryFilters().AsNoTracking()
                    : _db.Contenidos.AsNoTracking();

                // Lógica de filtro jerárquico:
                if (idSubcategoria.HasValue)
                {
                    baseQuery = baseQuery.Where(c =>
                        _db.ContenidosCategoriasRelacion
                           .AsNoTracking()
                           .Any(r => r.IdContenido == c.Id
                                     && r.Borrado == false
                                     && r.IdCategoria == idSubcategoria.Value));
                }
                else if (idCategoriaPadre.HasValue)
                {
                    var cats = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(c => c.Borrado == false &&
                                    (c.Sequence == idCategoriaPadre.Value || c.CategoriaPadre == idCategoriaPadre.Value))
                        .Select(c => c.Sequence)
                        .ToListAsync();

                    if (cats.Count > 0)
                    {
                        baseQuery = baseQuery.Where(c =>
                            _db.ContenidosCategoriasRelacion
                               .AsNoTracking()
                               .Any(r => r.IdContenido == c.Id
                                         && r.Borrado == false
                                         && r.IdCategoria.HasValue
                                         && cats.Contains(r.IdCategoria.Value)));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(c => false);
                    }
                }

                var recordsTotal = await baseQuery.CountAsync();

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    baseQuery = baseQuery.Where(c =>
                        (c.ContenidoTitulo ?? "").Contains(searchValue) ||
                        (c.ContenidoTextoC ?? "").Contains(searchValue));
                }

                var recordsFiltered = await baseQuery.CountAsync();

                // Excluir borradores por defecto, salvo que pedir mostrarlos
                if (!mostrarDraftsFlag)
                {
                    baseQuery = baseQuery.Where(c => (c.EstadoPublicacion ?? 0) != 0);
                }

                // Ordenamiento FechaCreado
                var orderColStr = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString();
                string orderColName = null;
                if (int.TryParse(orderColStr, out var orderColIdx))
                    orderColName = Request.Query[$"columns[{orderColIdx}][data]"].ToString();

                if (!string.IsNullOrWhiteSpace(orderColName) && orderColName.Equals("fechaCreado", StringComparison.OrdinalIgnoreCase))
                {
                    baseQuery = orderDir?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true
                        ? baseQuery.OrderBy(c => c.FechaCreado)
                        : baseQuery.OrderByDescending(c => c.FechaCreado);
                }
                else
                {
                    baseQuery = baseQuery.OrderByDescending(c => c.FechaCreado);
                }

                // Fetch page of contenidos including Tipo, image info and descripcion
                var pageItems = await baseQuery
                    .Skip(start)
                    .Take(length)
                    .Select(c => new
                    {
                        id = c.Id,
                        contenidoTitulo = c.ContenidoTitulo,
                        descripcion = c.ContenidoTextoC,
                        autor = c.Autor,
                        estadoPublicacion = c.EstadoPublicacion,
                        fechaCreado = c.FechaCreado,
                        eliminado = c.Eliminado,
                        uRLImagenPrincipal = c.URLImagenPrincipal,
                        hasImage = (c.URLImagenPrincipal != null && c.URLImagenPrincipal != ""),
                        tipo = c.IdTipo
                    })
                    .ToListAsync();

                var contentIds = pageItems.Select(i => i.id).Distinct().ToList();

                // Fetch latest category relation per content (in-memory grouping)
                var rels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Select(r => new { r.IdContenido, r.IdCategoria, r.FechaCreacion })
                    .ToListAsync();

                var catIds = rels.Select(r => r.IdCategoria.Value).Distinct().ToList();
                var categories = new Dictionary<int, string>();
                if (catIds.Any())
                {
                    var cats = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(c => catIds.Contains(c.Sequence))
                        .Select(c => new { c.Sequence, Nombre = c.Nombre ?? "" })
                        .ToListAsync();
                    categories = cats.ToDictionary(c => c.Sequence, c => c.Nombre);
                }

                var latestRelByContent = rels
                    .GroupBy(r => r.IdContenido)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.FechaCreacion).FirstOrDefault());

                // Build final projection including category name
                var data = pageItems.Select(pi =>
                {
                    string categoriaNombre = null;
                    if (latestRelByContent.TryGetValue(pi.id, out var rel) && rel != null && rel.IdCategoria.HasValue)
                    {
                        categories.TryGetValue(rel.IdCategoria.Value, out categoriaNombre);
                    }
                    return new
                    {
                        pi.id,
                        pi.contenidoTitulo,
                        descripcion = pi.descripcion ?? "",
                        pi.autor,
                        pi.estadoPublicacion,
                        pi.fechaCreado,
                        pi.eliminado,
                        pi.uRLImagenPrincipal,
                        pi.hasImage,
                        tipo = pi.tipo,
                        categoriaNombre = categoriaNombre ?? ""
                    };
                }).ToList();

                _logger.LogDebug("GridData (elim={Mostrar}, padre={Padre}, sub={Sub}, drafts={Drafts}) total={Total} filtered={Filtered} returned={Returned} search='{Search}'",
                    mostrarElimFlag, idCategoriaPadre, idSubcategoria, mostrarDraftsFlag, recordsTotal, recordsFiltered, data.Count, searchValue);

                return new JsonResult(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnGetGridDataAsync");
                return StatusCode(500, new { success = false, message = "Error interno al obtener datos." });
            }
        }

        // New handler: change status from modal (AJAX)
        public async Task<IActionResult> OnPostChangeStatusAsync()
        {
            try
            {
                if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]) || string.IsNullOrWhiteSpace(Request.Form["status"]))
                    return BadRequest(new { success = false, message = "ID o status inválido" });

                if (!int.TryParse(Request.Form["id"], out var id))
                    return BadRequest(new { success = false, message = "ID inválido" });

                if (!int.TryParse(Request.Form["status"], out var status))
                    return BadRequest(new { success = false, message = "Status inválido" });

                var entity = await _db.Contenidos.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
                if (entity == null) return NotFound(new { success = false, message = "Contenido no encontrado" });

                entity.EstadoPublicacion = status;
                entity.FechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true, status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnPostChangeStatusAsync");
                return StatusCode(500, new { success = false, message = "Error interno al cambiar estatus." });
            }
        }

        public async Task<IActionResult> OnGetGetContenidoAsync(int id)
        {
            try
            {
                if (id <= 0) return BadRequest(new { success = false, message = "ID inválido" });

                var dto = await _db.Contenidos
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ContenidoTitulo,
                        x.ContenidoTextoC,
                        x.ContenidoTextoL,
                        x.ContenidoTituloSlug,
                        x.URLImagenPrincipal,
                        x.EstadoPublicacion,
                        x.ContenidoFechaInicio,
                        x.ContenidoFechaFin,
                        x.IdAutor,
                        x.Autor,
                        x.IdEmpresa,
                        x.PaisClave,
                        x.IdUser,
                        eliminado = x.Eliminado
                    })
                    .FirstOrDefaultAsync();

                if (dto == null) return NotFound(new { success = false, message = "Contenido no encontrado" });

                var rel = await _db.ContenidosCategoriasRelacion
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(r => r.IdContenido == id && r.Borrado == false)
                    .OrderByDescending(r => r.FechaCreacion)
                    .FirstOrDefaultAsync();

                int? categoria = null;
                int? categoriaPadre = null;
                string categoriaNombre = "";

                if (rel?.IdCategoria != null)
                {
                    categoria = rel.IdCategoria.Value;
                    var cat = await _db.ContenidosCategorias
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .Where(c => c.Sequence == rel.IdCategoria.Value)
                        .Select(c => new { c.Sequence, Nombre = c.Nombre ?? "", c.CategoriaPadre })
                        .FirstOrDefaultAsync();
                    if (cat != null)
                    {
                        categoriaPadre = cat.CategoriaPadre;
                        categoriaNombre = cat.Nombre;
                    }
                }

                return new JsonResult(new
                {
                    id = dto.Id,
                    contenidoTitulo = dto.ContenidoTitulo,
                    contenidoTextoC = dto.ContenidoTextoC,
                    contenidoTextoL = dto.ContenidoTextoL,
                    contenidoTituloSlug = dto.ContenidoTituloSlug,
                    uRLImagenPrincipal = dto.URLImagenPrincipal,
                    estadoPublicacion = dto.EstadoPublicacion,
                    contenidoFechaInicio = dto.ContenidoFechaInicio,
                    contenidoFechaFin = dto.ContenidoFechaFin,
                    idAutor = dto.IdAutor,
                    autor = dto.Autor,
                    idEmpresa = dto.IdEmpresa,
                    paisClave = dto.PaisClave,
                    idUser = dto.IdUser,
                    eliminado = dto.eliminado,
                    categoria,
                    categoriaPadre,
                    categoriaNombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnGetGetContenidoAsync id={Id}", id);
                return StatusCode(500, new { success = false, message = "Error interno al obtener el contenido." });
            }
        }

        public async Task<IActionResult> OnPostEliminarContenidoAsync()
        {
            try
            {
                if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                    return BadRequest(new { success = false, message = "ID inválido" });

                if (!int.TryParse(Request.Form["id"], out var id))
                    return BadRequest(new { success = false, message = "ID inválido" });

                var entity = await _db.Contenidos
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) return new JsonResult(new { success = false, message = "Contenido no encontrado." });

                entity.Eliminado = true;
                entity.FechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnPostEliminarContenidoAsync");
                return StatusCode(500, new { success = false, message = "Error interno al eliminar contenido." });
            }
        }

        public async Task<IActionResult> OnPostRestaurarContenidoAsync()
        {
            try
            {
                if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                    return BadRequest(new { success = false, message = "ID inválido" });

                if (!int.TryParse(Request.Form["id"], out var id))
                    return BadRequest(new { success = false, message = "ID inválido" });

                var entity = await _db.Contenidos
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null) return new JsonResult(new { success = false, message = "Contenido no encontrado." });

                entity.Eliminado = false;
                entity.FechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnPostRestaurarContenidoAsync");
                return StatusCode(500, new { success = false, message = "Error interno al restaurar contenido." });
            }
        }
    }
}