using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Admin.Usuarios // <-- namespace corregido si usas Areas
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosIndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuariosIndexModel> _logger;

        public UsuariosIndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db, ILogger<UsuariosIndexModel> logger)
        {
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        public void OnGet() { }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetGridDataAsync()
        {
            // DataTables params (they come as query string)
            var draw = int.TryParse(Request.Query["draw"], out var dVal) ? dVal : 1;
            var start = int.TryParse(Request.Query["start"], out var sVal) ? sVal : 0;
            var length = int.TryParse(Request.Query["length"], out var lVal) ? lVal : 10;
            var searchValue = Request.Query["search[value]"].ToString();

            // Ordering
            var orderColumn = Request.Query["order[0][column]"].ToString();
            var orderDir = Request.Query["order[0][dir]"].ToString();

            // Field names in JS columns[] must be same order!
            string[] columnNames = { "email", "userName", "hashIsValid" };

            var usersQuery = _userManager.Users.AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                usersQuery = usersQuery.Where(u => u.Email.Contains(searchValue) || u.UserName.Contains(searchValue));
            }

            var recordsFiltered = await usersQuery.CountAsync();

            // PROYECCIÓN PRIMERO: crea la columna hashIsValid "real" para sortear correctamente
            var listQuery = usersQuery.Select(u => new
            {
                id = u.Id,
                email = u.Email,
                userName = u.UserName,
                hashIsValid = u.PasswordHash != null && u.PasswordHash.Length >= 50 && u.PasswordHash.StartsWith("AQAAAA")
            });

            // Ordenamiento sobre la proyección
            if (int.TryParse(orderColumn, out int colIndex))
            {
                switch (columnNames.ElementAtOrDefault(colIndex)?.ToLowerInvariant())
                {
                    case "email":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.email) : listQuery.OrderBy(u => u.email);
                        break;
                    case "username":
                        listQuery = orderDir == "desc" ? listQuery.OrderByDescending(u => u.userName) : listQuery.OrderBy(u => u.userName);
                        break;
                    case "hashisvalid":
                        listQuery = orderDir == "desc"
                            ? listQuery.OrderByDescending(u => u.hashIsValid)
                            : listQuery.OrderBy(u => u.hashIsValid);
                        break;
                    default:
                        listQuery = listQuery.OrderBy(u => u.email);
                        break;
                }
            }
            else
            {
                listQuery = listQuery.OrderBy(u => u.email);
            }

            // Paging
            var paged = await listQuery
                .Skip(start)
                .Take(length)
                .ToListAsync();

            return new JsonResult(new
            {
                draw,
                recordsTotal = await _userManager.Users.CountAsync(),
                recordsFiltered,
                data = paged
            });
        }

        // Ya no se usa IsHashValid, pero lo dejo si lo necesitas en el futuro.
        private static bool IsHashValid(string hash) =>
            !string.IsNullOrEmpty(hash)
            && hash.Length >= 50
            && hash.StartsWith("AQAAAA");

        public async Task<IActionResult> OnPostFillMissingSlugsAsync()
        {
            try
            {
                var toFix = await _db.Perfil
                    .Where(p => string.IsNullOrWhiteSpace(p.slug) && !string.IsNullOrWhiteSpace(p.Nombre))
                    .ToListAsync();

                var modified = 0;
                foreach (var perfil in toFix)
                {
                    var baseText = perfil.Nombre + (string.IsNullOrWhiteSpace(perfil.Apellidos) ? "" : " " + perfil.Apellidos);
                    var baseSlug = eiibd26.Helpers.SlugHelper.GenerateSlug(baseText);
                    var slug = baseSlug;
                    var counter = 1;
                    while (await _db.Perfil.AsNoTracking().AnyAsync(x => x.slug == slug))
                    {
                        slug = $"{baseSlug}-{counter}";
                        counter++;
                    }

                    perfil.slug = slug;
                    perfil.FechaModificado = DateTime.UtcNow;
                    modified++;
                }

                if (modified > 0)
                    await _db.SaveChangesAsync();

                return new JsonResult(new { modified });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generando slugs para usuarios");
                return new JsonResult(new { error = ex.Message });
            }
        }
    }
}