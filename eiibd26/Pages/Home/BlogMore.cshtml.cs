// eiibd26/Pages/Home/BlogMore.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Pages.Home
{
    public class BlogMoreModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public BlogMoreModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 3;

        public System.Collections.Generic.List<BlogItemVm> Items { get; set; } = new System.Collections.Generic.List<BlogItemVm>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 3;
            var skip = (PageNumber - 1) * PageSize;

            var items = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1)
                .OrderByDescending(c => c.FechaCreado)
                .Skip(skip)
                .Take(PageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                    Author = string.IsNullOrEmpty(c.Autor) ? "Autor" : c.Autor,
                    CreatedAt = c.FechaCreado
                })
                .ToListAsync();

            Items = items;
            return Page();
        }
    }
}