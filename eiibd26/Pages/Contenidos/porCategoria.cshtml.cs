using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System;

namespace eiibd26.Pages.Contenidos
{
    public class PorCategoriaModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PorCategoriaModel(ApplicationDbContext db) { _db = db; }

        [BindProperty(SupportsGet = true)]
        public string categorySegment { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 9;

        [BindProperty(SupportsGet = true)]
        public string ConditionIds { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SintomaIds { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TratamientoIds { get; set; }

        public List<int> FilterConditionIds { get; set; } = new List<int>();
        public List<int> FilterSintomaIds { get; set; } = new List<int>();
        public List<int> FilterTratamientoIds { get; set; } = new List<int>();

        public ContenidoporCategoria ContenidoPorCategoria { get; set; } = new ContenidoporCategoria();

        public int PageSizeView { get => ContenidoPorCategoria.PageSize; set => ContenidoPorCategoria.PageSize = value; }
        public int CategorySeq { get => ContenidoPorCategoria.CategorySeq; set => ContenidoPorCategoria.CategorySeq = value; }
        public string CategoryName { get => ContenidoPorCategoria.CategoryName; set => ContenidoPorCategoria.CategoryName = value; }
        public List<BlogItemVm> Items { get => ContenidoPorCategoria.Items; set => ContenidoPorCategoria.Items = value; }
        public int TotalCount { get => ContenidoPorCategoria.TotalCount; set => ContenidoPorCategoria.TotalCount = value; }

        public string CategorySegment => categorySegment ?? "";

        public List<BreadcrumbItem> Breadcrumbs { get; set; } = new List<BreadcrumbItem>();

        public class BreadcrumbItem
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public bool IsCurrent { get; set; }
        }

        public class TagVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public List<TagVm> AvailableConditions { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableSintomas { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableTratamientos { get; set; } = new List<TagVm>();

        private static List<int> ParseIds(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => { int.TryParse(s.Trim(), out var v); return v; })
                .Where(v => v > 0)
                .Distinct()
                .ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(categorySegment)) return NotFound();

            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 9;

            FilterConditionIds = ParseIds(ConditionIds);
            FilterSintomaIds = ParseIds(SintomaIds);
            FilterTratamientoIds = ParseIds(TratamientoIds);

            // Resolve target category
            ContenidoCategoria cat = null;
            if (int.TryParse(categorySegment, out var seq))
            {
                cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Sequence == seq && !c.Borrado);
            }
            else
            {
                // Case-insensitive comparison for slug
                var normalizedSegment = categorySegment?.ToLowerInvariant();
                cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoriaSlug.ToLower() == normalizedSegment && !c.Borrado);
            }

            if (cat == null) return NotFound();

            CategorySeq = cat.Sequence;
            CategoryName = cat.Nombre ?? $"Categoría {CategorySeq}";

            // Build breadcrumbs
            var crumbs = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Title = "Home", Url = "/", IsCurrent = false }
            };

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

            parents.Reverse();

            foreach (var p in parents)
            {
                var segment = !string.IsNullOrWhiteSpace(p.CategoriaSlug)
                    ? p.CategoriaSlug
                    : p.Sequence.ToString();

                crumbs.Add(new BreadcrumbItem
                {
                    Title = p.Nombre ?? $"Categoría {p.Sequence}",
                    Url = $"/{segment}",
                    IsCurrent = false
                });
            }

            crumbs.Add(new BreadcrumbItem
            {
                Title = CategoryName,
                Url = null,
                IsCurrent = true
            });

            Breadcrumbs = crumbs;

            // Load user-associated tags for UI filters
            await PopulateUserTagLists();

            // Resolve target category sequences (include children if parent)
            var targetCategorySeqs = new List<int> { cat.Sequence };

            if (!cat.CategoriaPadre.HasValue)
            {
                var children = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => c.CategoriaPadre == cat.Sequence && !c.Borrado)
                    .Select(c => c.Sequence)
                    .ToListAsync();

                if (children.Any())
                    targetCategorySeqs.AddRange(children);
            }

            // Get distinct content IDs from category relation table (support multiple category relations per content)
            var distinctIds = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => !r.Borrado &&
                           r.IdCategoria != null &&
                           targetCategorySeqs.Contains(r.IdCategoria.Value))
                .Select(r => r.IdContenido)
                .Distinct()
                .ToListAsync();

            if (!distinctIds.Any())
            {
                Items = new List<BlogItemVm>();
                TotalCount = 0;
                return Page();
            }

            // Apply filters (AND logic)
            try
            {
                if (FilterConditionIds.Any())
                {
                    var condContentIds = await _db.ContenidoCondiciones
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado &&
                                     FilterConditionIds.Contains(rel.CondicionId) &&
                                     distinctIds.Contains(rel.ContenidoId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    distinctIds = distinctIds.Intersect(condContentIds).ToList();
                }

                if (FilterSintomaIds.Any())
                {
                    var sintContentIds = await _db.ContenidoSintomas
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado &&
                                     FilterSintomaIds.Contains(rel.SintomaId) &&
                                     distinctIds.Contains(rel.ContenidoId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    distinctIds = distinctIds.Intersect(sintContentIds).ToList();
                }

                if (FilterTratamientoIds.Any())
                {
                    var tratContentIds = await _db.ContenidoTratamientos
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado &&
                                     FilterTratamientoIds.Contains(rel.TratamientoId) &&
                                     distinctIds.Contains(rel.ContenidoId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    distinctIds = distinctIds.Intersect(tratContentIds).ToList();
                }
            }
            catch
            {
                // Ignore filter errors
            }

            if (!distinctIds.Any())
            {
                Items = new List<BlogItemVm>();
                TotalCount = 0;
                return Page();
            }

            // Allow publication states 1, 2, 3
            var allowedStatuses = new[] { 1, 2, 3 };

            TotalCount = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado &&
                           allowedStatuses.Contains((c.EstadoPublicacion ?? 0)) &&
                           distinctIds.Contains(c.Id))
                .CountAsync();

            var items = await _db.Contenidos
                .AsNoTracking()
                .Include(c => c.AutorPerfil)
                .Where(c => !c.Eliminado &&
                           allowedStatuses.Contains((c.EstadoPublicacion ?? 0)) &&
                           distinctIds.Contains(c.Id))
                .OrderByDescending(c => c.FechaCreado)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Slug = c.ContenidoTituloSlug ?? "",
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal)
                        ? null
                        : "/uploads/contenidos/" + c.URLImagenPrincipal,
                    Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre)) ? c.AutorPerfil.Nombre : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                    AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar))
                        ? c.AutorPerfil.Avatar
                        : (string)null,
                    AuthorSlug = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.slug)) ? c.AutorPerfil.slug : "",
                    AuthorId = (c.AutorPerfil != null) ? c.AutorPerfil.idUser : (Guid?)null,
                    CreatedAt = c.FechaCreado
                })
                .ToListAsync();

            await AttachCategoriesAsync(items);

            Items = items;
            return Page();
        }

        private async Task PopulateUserTagLists()
        {
            AvailableConditions = new List<TagVm>();
            AvailableSintomas = new List<TagVm>();
            AvailableTratamientos = new List<TagVm>();

            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userGuid))
            {
                return;
            }

            try
            {
                var userCondIds = await _db.condicionUsuario
                    .AsNoTracking()
                    .Where(x => x.Usuario.Id == userGuid && !x.Eliminado)
                    .Select(x => x.Condicion.id)
                    .Distinct()
                    .ToListAsync();

                if (userCondIds.Any())
                {
                    AvailableConditions = await _db.condiciones
                        .AsNoTracking()
                        .Where(c => userCondIds.Contains(c.id) && c.idPadre != null)
                        .OrderBy(c => c.nombre)
                        .Select(c => new TagVm { Id = c.id, Name = c.nombre })
                        .ToListAsync();
                }

                var userSintIds = await _db.sintomasUsuario
                    .AsNoTracking()
                    .Where(x => x.Usuario.Id == userGuid && !x.Eliminado)
                    .Select(x => x.Sintoma.id)
                    .Distinct()
                    .ToListAsync();

                if (userSintIds.Any())
                {
                    AvailableSintomas = await _db.sintomas
                        .AsNoTracking()
                        .Where(s => userSintIds.Contains(s.id))
                        .OrderBy(s => s.nombre)
                        .Select(s => new TagVm { Id = s.id, Name = s.nombre })
                        .ToListAsync();
                }

                var userTratIds = await _db.tratamientoUsuario
                    .AsNoTracking()
                    .Where(x => x.Usuario.Id == userGuid && !x.Eliminado)
                    .Select(x => x.Tratamiento.id)
                    .Distinct()
                    .ToListAsync();

                if (userTratIds.Any())
                {
                    AvailableTratamientos = await _db.tratamientos
                        .AsNoTracking()
                        .Where(t => userTratIds.Contains(t.id) && t.idPadre != null)
                        .OrderBy(t => t.nombre)
                        .Select(t => new TagVm { Id = t.id, Name = t.nombre })
                        .ToListAsync();
                }
            }
            catch
            {
                AvailableConditions = new List<TagVm>();
                AvailableSintomas = new List<TagVm>();
                AvailableTratamientos = new List<TagVm>();
            }
        }

        private async Task AttachCategoriesAsync(List<BlogItemVm> list)
        {
            if (list == null || !list.Any()) return;

            var ids = list.Select(x => x.Id).ToList();

            // Get category relations for all items, prioritizing primary categories
            var catRels = await _db.ContenidosCategoriasRelacion
                .AsNoTracking()
                .Where(r => ids.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                .Join(_db.ContenidosCategorias.AsNoTracking(),
                      rel => rel.IdCategoria,
                      cat => cat.Sequence,
                      (rel, cat) => new {
                          rel.IdContenido,
                          rel.EsPrincipal,
                          cat.Sequence,
                          cat.CategoriaSlug,
                          cat.Nombre,
                          cat.CategoriaPadre
                      })
                .ToListAsync();

            // Build map: prefer primary category, then child categories over parent
            var map = catRels
                .GroupBy(x => x.IdContenido)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        // First try to find primary category
                        var primary = g.FirstOrDefault(x => x.EsPrincipal == true);
                        if (primary != null)
                        {
                            var segment = !string.IsNullOrWhiteSpace(primary.CategoriaSlug)
                                ? primary.CategoriaSlug
                                : primary.Sequence.ToString();
                            return (Name: primary.Nombre, Slug: segment, Id: (int?)primary.Sequence);
                        }

                        // Otherwise, prefer child categories over parent
                        var chosen = g
                            .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                            .ThenBy(x => x.Sequence)
                            .FirstOrDefault();

                        if (chosen == null)
                            return (Name: (string)null, Slug: (string)null, Id: (int?)null);

                        var seg = !string.IsNullOrWhiteSpace(chosen.CategoriaSlug)
                            ? chosen.CategoriaSlug
                            : chosen.Sequence.ToString();

                        return (Name: chosen.Nombre, Slug: seg, Id: (int?)chosen.Sequence);
                    }
                );

            // Fallback: try SubCat and CatPadre columns for items without relations
            var missingIds = ids.Where(id => !map.ContainsKey(id)).ToList();

            if (missingIds.Any())
            {
                var contents = await _db.Contenidos
                    .AsNoTracking()
                    .Where(c => missingIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        SubCat = EF.Property<int?>(c, "SubCat"),
                        CatPadre = EF.Property<int?>(c, "CatPadre")
                    })
                    .ToListAsync();

                var subCatIds = contents
                    .Select(c => c.SubCat)
                    .Where(v => v.HasValue)
                    .Select(v => v.Value)
                    .Distinct()
                    .ToList();

                if (subCatIds.Any())
                {
                    var cats = await _db.ContenidosCategorias
                        .AsNoTracking()
                        .Where(cat => subCatIds.Contains(cat.Sequence) && !cat.Borrado)
                        .Select(cat => new {
                            cat.Sequence,
                            cat.Nombre,
                            cat.CategoriaSlug,
                            cat.CategoriaPadre
                        })
                        .ToListAsync();

                    var catsBySeq = cats.ToDictionary(c => c.Sequence, c => c);

                    foreach (var row in contents)
                    {
                        if (!row.SubCat.HasValue) continue;

                        if (catsBySeq.TryGetValue(row.SubCat.Value, out var cat))
                        {
                            var seg = !string.IsNullOrWhiteSpace(cat.CategoriaSlug)
                                ? cat.CategoriaSlug
                                : cat.Sequence.ToString();

                            map[row.Id] = (Name: cat.Nombre, Slug: seg, Id: (int?)cat.Sequence);
                        }
                    }
                }

                var stillMissing = ids.Where(id => !map.ContainsKey(id)).ToList();

                if (stillMissing.Any())
                {
                    var contents2 = await _db.Contenidos
                        .AsNoTracking()
                        .Where(c => stillMissing.Contains(c.Id))
                        .Select(c => new {
                            c.Id,
                            CatPadre = EF.Property<int?>(c, "CatPadre")
                        })
                        .ToListAsync();

                    var parentIds = contents2
                        .Select(c => c.CatPadre)
                        .Where(v => v.HasValue)
                        .Select(v => v.Value)
                        .Distinct()
                        .ToList();

                    if (parentIds.Any())
                    {
                        var parentCats = await _db.ContenidosCategorias
                            .AsNoTracking()
                            .Where(cat => parentIds.Contains(cat.Sequence) && !cat.Borrado)
                            .Select(cat => new {
                                cat.Sequence,
                                cat.Nombre,
                                cat.CategoriaSlug
                            })
                            .ToListAsync();

                        var parentBySeq = parentCats.ToDictionary(c => c.Sequence, c => c);

                        foreach (var row in contents2)
                        {
                            if (!row.CatPadre.HasValue) continue;

                            if (parentBySeq.TryGetValue(row.CatPadre.Value, out var cat))
                            {
                                var seg = !string.IsNullOrWhiteSpace(cat.CategoriaSlug)
                                    ? cat.CategoriaSlug
                                    : cat.Sequence.ToString();

                                map[row.Id] = (Name: cat.Nombre, Slug: seg, Id: (int?)cat.Sequence);
                            }
                        }
                    }
                }
            }

            // Assign categories to items
            foreach (var it in list)
            {
                if (map.TryGetValue(it.Id, out var v) &&
                    !string.IsNullOrWhiteSpace(v.Name) &&
                    !string.IsNullOrWhiteSpace(v.Slug))
                {
                    it.Category = $"<a href=\"/{v.Slug}\" class=\"blog-category\">{v.Name}</a>";
                    it.PrimaryCategorySlug = v.Slug;
                    it.PrimaryCategoryId = v.Id;
                }
            }
        }
    }
}