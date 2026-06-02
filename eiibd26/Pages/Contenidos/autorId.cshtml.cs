using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;

namespace eiibd26.Pages.Contenidos
{
    public class AutorIdModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public AutorIdModel(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var perfil = await _db.Perfil
                .FirstOrDefaultAsync(p => p.idUser == id);

            if (perfil == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(perfil.slug))
                return RedirectPermanent($"/contenidos/porAutor/{perfil.slug}");

            var slug = await SlugHelper.GenerateUniqueSlugForUsuario(_db, perfil.Nombre ?? "autor", perfil.idUser);
            perfil.slug = slug;
            await _db.SaveChangesAsync();

            return RedirectPermanent($"/contenidos/porAutor/{slug}");
        }
    }
}
