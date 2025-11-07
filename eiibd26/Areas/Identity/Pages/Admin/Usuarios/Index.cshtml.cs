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

        public UsuariosIndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
    }
}