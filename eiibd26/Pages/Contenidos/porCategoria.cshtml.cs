using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;

namespace eiibd26.Pages.Contenidos
{
    public class PorCategoriaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PorCategoriaModel(ApplicationDbContext db) { _db = db; }

        // Route segment (slug or numeric)
        [BindProperty(SupportsGet = true)]
        public string categorySegment { get; set; }

        // Container view-model requested to live in Models/
        public ContenidoporCategoria ContenidoPorCategoria { get; set; } = new ContenidoporCategoria();

        // Backwards-compatible properties
        public int PageNumber { get => ContenidoPorCategoria.PageNumber; set => ContenidoPorCategoria.PageNumber = value; }
        public int PageSize { get => ContenidoPorCategoria.PageSize; set => ContenidoPorCategoria.PageSize = value; }
        public int CategorySeq { get => ContenidoPorCategoria.CategorySeq; set => ContenidoPorCategoria.CategorySeq = value; }
        public string CategoryName { get => ContenidoPorCategoria.CategoryName; set => ContenidoPorCategoria.CategoryName = value; }
        public List<BlogItemVm> Items { get => ContenidoPorCategoria.Items; set => ContenidoPorCategoria.Items = value; }
        public int TotalCount { get => ContenidoPorCategoria.TotalCount; set => ContenidoPorCategoria.TotalCount = value; }

        // Expose the incoming segment for view (used by JS)
        public string CategorySegment => categorySegment ?? "";

        // Breadcrumb items
        public List<BreadcrumbItem> Breadcrumbs { get; set; } = new List<BreadcrumbItem>();

        public class BreadcrumbItem
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public bool IsCurrent { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(categorySegment)) return NotFound();

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 7;
            var skip = (PageNumber - 1) * PageSize;

            // Resolver categoría por segment: si es número -> sequence, sino -> slug
            ContenidoCategoria cat = null;
            if (int.TryParse(categorySegment, out var seq))
            {
                cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Sequence == seq && !c.Borrado);
            }
            else
            {
                cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoriaSlug == categorySegment && !c.Borrado);
            }

            if (cat == null) return NotFound();

            CategorySeq = cat.Sequence;
            CategoryName = cat.Nombre ?? $"Categoría {CategorySeq}";

            // Construir breadcrumbs:
            // Home -> (padres en orden) -> categoría actual (sin link)
            var crumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Title = "Home", Url = Url.Content("~/"), IsCurrent = false }
            };

            // recolectar padres (stack) siguiendo CategoriaPadre
            var parents = new List<ContenidoCategoria>();
            var parentSeq = cat.CategoriaPadre;
            while (parentSeq.HasValue && parentSeq.Value > 0)
            {
                var p = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Sequence == parentSeq.Value && !x.Borrado);

                if (p == null) break;
                parents.Add(p);
                parentSeq = p.CategoriaPadre;
            }

            parents.Reverse(); // del más alto al más cercano

            foreach (var p in parents)
            {
                var segment = !string.IsNullOrWhiteSpace(p.CategoriaSlug) ? p.CategoriaSlug : p.Sequence.ToString();
                crumbs.Add(new BreadcrumbItem
                {
                    Title = p.Nombre ?? $"Categoría {p.Sequence}",
                    Url = Url.Content($"/Contenidos/categoria/{segment}"),
                    IsCurrent = false
                });
            }

            // categoría actual (sin link)
            crumbs.Add(new BreadcrumbItem { Title = CategoryName, Url = null, IsCurrent = true });

            Breadcrumbs = crumbs;

            // 1) Obtener ids distintos de contenidos relacionados (desde la tabla de relación)
            var distinctIds = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => !r.Borrado && r.IdCategoria == CategorySeq)
                .Select(r => r.IdContenido)
                .Distinct()
                .ToListAsync();

            if (!distinctIds.Any())
            {
                Items = new List<BlogItemVm>();
                TotalCount = 0;
                return Page();
            }

            // 2) Contar totales (aplicando filtro de no eliminado y publicado)
            TotalCount = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1 && distinctIds.Contains(c.Id))
                .CountAsync();

            // 3) Traer entidades usando Contains sobre los ids (ordenar / paginar), aplicando filtro de publicación
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
                    Category = CategoryName
                })
                .ToListAsync();

            Items = items;
            return Page();
        }
    }
}