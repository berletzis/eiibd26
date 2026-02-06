using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

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
                        tipo = c.IdTipo,
                        relevante = c.EstadoPublicacion,
                        categoria = "", // filled below
                        autor = c.Autor,
                        publicado = c.EstadoPublicacion,
                        fechaCreado = c.FechaCreado,
                        eliminado = c.Eliminado,
                        imagenUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal)
                    })
                    .ToListAsync();

                // Resolve primary category for display (like other pages)
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

                // Build actions HTML (Edit + Delete (soft) + Clone form)
                var data = pageItems.Select(p =>
                {
                    var catName = catMap.TryGetValue(p.id, out var v) && !string.IsNullOrWhiteSpace(v.Name) ? v.Name : "";
                    var editUrl = Url.Page("./Detalle", new { id = p.id });

                    var deleteForm = $"<form method='post' action='{Url.Page("./Index")}?handler=Eliminar' style='display:inline;margin-left:6px' onsubmit=\"return confirm('Marcar este contenido como eliminado?');\">" +
                                     $"<input type='hidden' name='id' value='{p.id}' />" +
                                     $"<button type='submit' class='btn btn-sm btn-outline-danger'>Eliminar</button>" +
                                     $"</form>";

                    var cloneForm = $"<form method='post' action='{Url.Page("./Index")}?handler=Clone' style='display:inline;margin-left:6px' onsubmit=\"return confirm('Clonar este contenido?')\">" +
                                    $"<input type='hidden' name='id' value='{p.id}' />" +
                                    $"<button type='submit' class='btn btn-sm btn-outline-secondary'>Clonar</button>" +
                                    $"</form>";
                    var editBtn = $"<a class='btn btn-sm btn-outline-primary' href='{editUrl}'>Editar</a>";

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
                _logger.LogError(ex, "Error generando grid data");
                return new JsonResult(new { draw = 0, recordsTotal = 0, recordsFiltered = 0, data = new object[0] });
            }
        }

        // POST handler: clonar contenido con todas sus relaciones
        public async Task<IActionResult> OnPostCloneAsync(int id)
        {
            try
            {
                // Load original content
                var orig = await _db.Contenidos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
                if (orig == null) return NotFound();

                var now = DateTime.UtcNow;
                var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid userGuid = Guid.TryParse(rawUserId, out var g) ? g : Guid.Empty;

                // Create clone
                var clone = new Models.Contenido
                {
                    ContenidoTitulo = "Clonado / " + (orig.ContenidoTitulo ?? ""),
                    ContenidoTituloSlug = null, // leave slug for manual edit / regenerate
                    ContenidoTextoC = orig.ContenidoTextoC,
                    ContenidoTextoL = orig.ContenidoTextoL,
                    IdTipo = orig.IdTipo,
                    URLImagenPrincipal = orig.URLImagenPrincipal,
                    EstadoPublicacion = 0, // Draft
                    ContenidoFechaInicio = orig.ContenidoFechaInicio,
                    ContenidoFechaFin = orig.ContenidoFechaFin,
                    IdAutor = orig.IdAutor,
                    Autor = orig.Autor,
                    PaisClave = orig.PaisClave,
                    UsuarioCreacion = userGuid,
                    UsuarioModificacion = userGuid,
                    FechaCreado = now,
                    FechaModificado = now,
                    Eliminado = false
                };

                _db.Contenidos.Add(clone);
                await _db.SaveChangesAsync();

                var newId = clone.Id;

                // Copy category relations
                var origCatRels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => r.IdContenido == id && !r.Borrado && r.IdCategoria != null)
                    .Select(r => r.IdCategoria.Value)
                    .Distinct()
                    .ToListAsync();

                foreach (var catId in origCatRels)
                {
                    _db.ContenidosCategoriasRelacion.Add(new Models.ContenidoCategoriaRelacion
                    {
                        IdContenido = newId,
                        IdCategoria = catId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        Borrado = false
                    });
                }

                // Copy contenidos relacionados
                var origContRels = await _db.ContenidosRelacionados
                    .AsNoTracking()
                    .Where(r => r.IdContenido == id && !r.Borrado)
                    .ToListAsync();
                foreach (var r in origContRels)
                {
                    _db.ContenidosRelacionados.Add(new Models.ContenidoRelacionado
                    {
                        IdContenido = newId,
                        IdContenidoRelacionado = r.IdContenidoRelacionado,
                        Tipo = r.Tipo,
                        Descripcion = r.Descripcion,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        Borrado = false
                    });
                }

                // Copy preguntas relacionadas
                var origPregRels = await _db.ContenidosPreguntasRelacion
                    .AsNoTracking()
                    .Where(r => r.ContenidoId == id && !r.Borrado)
                    .ToListAsync();
                foreach (var r in origPregRels)
                {
                    _db.ContenidosPreguntasRelacion.Add(new Models.ContenidoPreguntaRelacion
                    {
                        ContenidoId = newId,
                        PreguntaId = r.PreguntaId,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        Borrado = false
                    });
                }

                // Copy domain relations: condiciones
                var origCond = await _db.ContenidoCondiciones.AsNoTracking()
                    .Where(x => x.ContenidoId == id && !x.Borrado)
                    .ToListAsync();
                foreach (var r in origCond)
                {
                    _db.ContenidoCondiciones.Add(new Models.ContenidoCondicion
                    {
                        ContenidoId = newId,
                        CondicionId = r.CondicionId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        Borrado = false
                    });
                }

                // Síntomas
                var origSint = await _db.ContenidoSintomas.AsNoTracking()
                    .Where(x => x.ContenidoId == id && !x.Borrado)
                    .ToListAsync();
                foreach (var r in origSint)
                {
                    _db.ContenidoSintomas.Add(new Models.ContenidoSintoma
                    {
                        ContenidoId = newId,
                        SintomaId = r.SintomaId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        Borrado = false
                    });
                }

                // Tratamientos
                var origTrat = await _db.ContenidoTratamientos.AsNoTracking()
                    .Where(x => x.ContenidoId == id && !x.Borrado)
                    .ToListAsync();
                foreach (var r in origTrat)
                {
                    _db.ContenidoTratamientos.Add(new Models.ContenidoTratamiento
                    {
                        ContenidoId = newId,
                        TratamientoId = r.TratamientoId,
                        FechaCreacion = now,
                        FechaModificacion = now,
                        UsuarioCreacion = userGuid,
                        UsuarioModificacion = userGuid,
                        Borrado = false
                    });
                }

                await _db.SaveChangesAsync();

                // Redirect to editor of the cloned content
                return RedirectToPage("./Detalle", new { id = newId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clonando contenido {Id}", id);
                TempData["Error"] = "Error al clonar el contenido.";
                return RedirectToPage();
            }
        }

        // POST handler: soft delete content
        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            try
            {
                var ent = await _db.Contenidos.FirstOrDefaultAsync(c => c.Id == id && !c.Eliminado);
                if (ent == null)
                {
                    TempData["Error"] = "Contenido no encontrado o ya eliminado.";
                    return RedirectToPage();
                }

                ent.Eliminado = true;
                ent.FechaModificado = DateTime.UtcNow;
                ent.UsuarioModificacion = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : (Guid?)null;

                _db.Contenidos.Update(ent);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Contenido marcado como eliminado (soft-delete).";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando contenido {Id}", id);
                TempData["Error"] = "Error al eliminar el contenido.";
                return RedirectToPage();
            }
        }
    }
}