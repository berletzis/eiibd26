// eiibd26/Pages/Home/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) { _db = db; }

        public HeroViewModel Hero { get; set; } = new HeroViewModel();
        public BlogListViewModel BlogList { get; set; } = new BlogListViewModel();

        public async Task OnGetAsync()
        {
            // Hero placeholder
            Hero.Title = "Just a Blog Floating Through the Noise";
            Hero.Subtitle = "A quiet space in the noise — drifting thoughts, small truths, and everything in between.";
            Hero.CallToAction = "Contact Me";

            const int pageSize = 7;
            BlogList.PageSize = pageSize;
            BlogList.PageNumber = 1;

            BlogList.TotalCount = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1)
                .CountAsync();

            var items = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1)
                .OrderByDescending(c => c.FechaCreado)
                .Take(pageSize)
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

            BlogList.Items = items;
        }
    }
}