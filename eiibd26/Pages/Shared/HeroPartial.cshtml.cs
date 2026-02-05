using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace eiibd26.Pages.Shared
{
    public class HeroPartialModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public HeroPartialModel(ApplicationDbContext db) { _db = db; }

        public List<BannerVm> Slides { get; set; } = new List<BannerVm>();

        public string DefaultTitle { get; set; } = "#HazViralLoQueImporta";
        public string DefaultSubtitle { get; set; } = "Analizamos datos relacionados la Enfermedad Inflamatoria Intestinal";

        public class BannerVm
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public string? Subtitle { get; set; }
            public string? ImagePath { get; set; }
            public bool IsImage { get; set; }
        }

        public async Task OnGetAsync()
        {
            bool isAuth = User?.Identity?.IsAuthenticated == true;

            // LINQ/EF: obtener hasta 4 banners visibles y no borrados
            var query = _db.BannersInicio
                .AsNoTracking()
                .Where(b => b.IsActive && !b.Borrado &&
                       (isAuth ? b.VisibleToAuthenticated : b.VisibleToAnonymous))
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Id)
                .Take(4)
                .Select(b => new BannerVm
                {
                    Id = b.Id,
                    Title = b.Title,
                    Subtitle = b.Subtitle,
                    ImagePath = b.ImagePath,
                    IsImage = b.IsImage
                });

            Slides = await query.ToListAsync();

            // fallback hasta 4 si no hay suficientes
            while (Slides.Count < 4)
            {
                var idx = Slides.Count;
                Slides.Add(new BannerVm
                {
                    Id = -1 * (idx + 1),
                    Title = idx == 0 ? DefaultTitle : (idx == 1 ? "Únete y comparte" : "Comunidad y datos"),
                    Subtitle = idx == 0 ? DefaultSubtitle : (idx == 1 ? "Regístrate para participar en la comunidad" : "Comparte experiencias y soluciones"),
                    ImagePath = null,
                    IsImage = false
                });
            }

            if (Slides.Count > 4) Slides = Slides.GetRange(0, 4);
        }
    }
}