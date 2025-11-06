using eiibd26.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Models;


namespace eiibd26.Pages.Admin.Usuarios
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

        public async Task<IActionResult> OnGetGridDataAsync(int draw, int start, int length)
        {
            var searchValue = Request.Query["search[value]"].ToString();
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                query = query.Where(u => u.Email.Contains(searchValue) || u.UserName.Contains(searchValue));
            }

            var totalCount = await query.CountAsync();

            var usuarios = await query
                .OrderBy(u => u.Email)
                .Skip(start)
                .Take(length)
                .Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    userName = u.UserName,
                    hashIsValid = IsHashValid(u.PasswordHash)
                })
                .ToListAsync();

            return new JsonResult(new
            {
                draw,
                recordsTotal = totalCount,
                recordsFiltered = totalCount,
                data = usuarios
            });
        }

        private static bool IsHashValid(string hash) =>
            !string.IsNullOrEmpty(hash)
            && hash.Length >= 50
            && hash.StartsWith("AQAAAA");
    }
}