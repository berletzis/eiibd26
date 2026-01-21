using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Pages.Shared
{
    public class _FooterPartialModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public _FooterPartialModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // Lists exposed to the view
        public List<ContenidoCategoria> Categorias12 { get; set; } = new();
        public List<ContenidoCategoria> Categorias47 { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Column 2: categories where (Sequence == 12 OR CategoriaPadre == 12) AND Relevante == true AND not deleted
            Categorias12 = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado
                            && c.Relevante == true
                            && (c.Sequence == 12 || c.CategoriaPadre == 12))
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            // Column 3: contents where (Sequence == 47 OR CategoriaPadre == 47) AND Relevante == true AND not deleted
            Categorias47 = await _db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => !c.Borrado
                            && c.Relevante == true
                            && (c.Sequence == 47 || c.CategoriaPadre == 47))
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }
    }
}