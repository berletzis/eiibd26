using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace eiibd26.Areas.Identity.Pages.Admin.Contenidos
{
    // Mantengo IgnoreAntiforgeryToken para evitar problemas con token en AJAX.
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

        public void OnGet()
        {
            // Nada que inicializar server-side; la tabla será cargada por DataTables.
        }

        // DataTables server-side: devuelve { draw, recordsTotal, recordsFiltered, data }
        public async Task<IActionResult> OnGetGridDataAsync(bool mostrarEliminados = false)
        {
            try
            {
                var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
                var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
                var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
                var searchValue = (Request.Query["search[value]"].ToString() ?? "").Trim();

                // Detectar mostrarEliminados de forma robusta (d.mostrarEliminados)
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

                bool mostrarElimFlag = mostrarEliminados;
                if (!string.IsNullOrEmpty(mostrarElimQuery))
                {
                    mostrarElimFlag = mostrarElimQuery == "1"
                        || mostrarElimQuery.Equals("true", StringComparison.OrdinalIgnoreCase)
                        || mostrarElimQuery.Equals("on", StringComparison.OrdinalIgnoreCase);
                }

                _logger.LogDebug("GridData request. QueryString={Query} resolved mostrarEliminados='{Param}' => {Flag} (draw={Draw}, start={Start}, len={Len}, search='{Search}')",
                    Request.QueryString.Value, mostrarElimQuery, mostrarElimFlag, draw, start, length, searchValue);

                // recordsTotal sin filtro
                var recordsTotal = await _db.Contenidos.AsNoTracking().CountAsync();

                // Base query (usa la propiedad pública Eliminado)
                IQueryable<Models.Contenido> q = _db.Contenidos.AsNoTracking();
                if (!mostrarElimFlag)
                {
                    q = q.Where(c => !c.Eliminado);
                }

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    var s = searchValue;
                    q = q.Where(c => (c.ContenidoTitulo ?? "").Contains(s) || (c.ContenidoTextoC ?? "").Contains(s));
                }

                var recordsFiltered = await q.CountAsync();

                var data = await q
                    .OrderByDescending(c => c.FechaCreado)
                    .Skip(start)
                    .Take(length)
                    .Select(c => new
                    {
                        id = c.Id,
                        contenidoTitulo = c.ContenidoTitulo,
                        autor = c.Autor,
                        estadoPublicacion = c.EstadoPublicacion,
                        fechaCreado = c.FechaCreado,
                        eliminado = c.Eliminado
                    })
                    .ToListAsync();

                _logger.LogDebug("GridData result: total={Total}, filtered={Filtered}, returned={Returned}", recordsTotal, recordsFiltered, data.Count);

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
                // devolver JSON con error y código 500
                return StatusCode(500, new { success = false, message = "Error interno al obtener datos." });
            }
        }

        // GET one content (for view modal or to pre-fill Detalle page)
        public async Task<IActionResult> OnGetGetContenidoAsync(int id)
        {
            try
            {
                if (id <= 0) return BadRequest(new { success = false, message = "ID inválido" });

                var dto = await _db.Contenidos
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

                // obtener relación de categoría más reciente si existe
                var rel = await _db.Set<Models.ContenidoCategoriaRelacion>()
                    .AsNoTracking()
                    .Where(r => r.IdContenido == id && (EF.Property<bool?>(r, "Borrado") ?? false) == false)
                    .OrderByDescending(r => r.FechaCreacion)
                    .FirstOrDefaultAsync();

                int? categoria = null;
                int? categoriaPadre = null;
                string categoriaNombre = "";

                if (rel != null && rel.IdCategoria.HasValue)
                {
                    categoria = rel.IdCategoria.Value;
                    var cat = await _db.Set<Models.ContenidoCategoria>()
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

        // POST soft-delete
        public async Task<IActionResult> OnPostEliminarContenidoAsync()
        {
            try
            {
                if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                    return BadRequest(new { success = false, message = "ID inválido" });

                if (!int.TryParse(Request.Form["id"], out var id)) return BadRequest(new { success = false, message = "ID inválido" });

                var entity = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null) return new JsonResult(new { success = false, message = "Contenido no encontrado." });

                entity.Eliminado = true;
                entity.FechaModificado = DateTime.UtcNow;
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userGuid))
                {
                    entity.UsuarioModificacion = userGuid;
                }

                await _db.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OnPostEliminarContenidoAsync");
                return StatusCode(500, new { success = false, message = "Error interno al eliminar contenido." });
            }
        }

        // POST restore
        public async Task<IActionResult> OnPostRestaurarContenidoAsync()
        {
            try
            {
                if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"]))
                    return BadRequest(new { success = false, message = "ID inválido" });

                if (!int.TryParse(Request.Form["id"], out var id)) return BadRequest(new { success = false, message = "ID inválido" });

                var entity = await _db.Contenidos.FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null) return new JsonResult(new { success = false, message = "Contenido no encontrado." });

                entity.Eliminado = false;
                entity.FechaModificado = DateTime.UtcNow;
                var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userGuid))
                {
                    entity.UsuarioModificacion = userGuid;
                }

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