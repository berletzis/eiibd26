using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;

namespace eiibd26.Pages.Home
{
    public class BlogMoreModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public BlogMoreModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 3;

        // Parámetros opcionales para filtrar por categoría
        [BindProperty(SupportsGet = true)] public int? CategorySeq { get; set; }
        [BindProperty(SupportsGet = true)] public string CategorySlug { get; set; }

        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 3;
            var skip = (PageNumber - 1) * PageSize;

            // Si llega categorySlug, resolver sequence y nombre
            string resolvedCategoryName = null;
            int? resolvedCategorySeq = CategorySeq;
            if (!string.IsNullOrWhiteSpace(CategorySlug))
            {
                var cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoriaSlug == CategorySlug && !c.Borrado);

                if (cat == null)
                {
                    Items = new List<BlogItemVm>();
                    return Page();
                }

                resolvedCategorySeq = cat.Sequence;
                resolvedCategoryName = cat.Nombre ?? "";
            }
            else if (CategorySeq.HasValue)
            {
                var cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Sequence == CategorySeq.Value && !c.Borrado);

                if (cat != null) resolvedCategoryName = cat.Nombre ?? "";
            }

            if (resolvedCategorySeq.HasValue)
            {
                // Filtrar por categoría: obtener ids distintos desde la tabla de relación
                var distinctIds = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria == resolvedCategorySeq.Value)
                    .Select(r => r.IdContenido)
                    .Distinct()
                    .ToListAsync();

                if (!distinctIds.Any())
                {
                    Items = new List<BlogItemVm>();
                    return Page();
                }

                // traer entidades filtradas (no eliminadas y publicadas), ordenar y paginar
                var items = await _db.Contenidos
                    .AsNoTracking()
                    .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1 && distinctIds.Contains(c.Id))
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
                        CreatedAt = c.FechaCreado,
                        Category = resolvedCategoryName ?? ""
                    })
                    .ToListAsync();

                Items = items;
                return Page();
            }
            else
            {
                // Comportamiento original (sin filtrar por categoría)
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
                        CreatedAt = c.FechaCreado,
                        Category = "" // opcional
                    })
                    .ToListAsync();

                Items = items;
                return Page();
            }
        }
    }
}