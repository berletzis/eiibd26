using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Models;

namespace eiibd26.Pages.Contenidos
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) { _db = db; }

        // Paging and filters
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 9;

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        // Raw comma-separated inputs from query string
        [BindProperty(SupportsGet = true)]
        public string ConditionIds { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SintomaIds { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TratamientoIds { get; set; }

        // Parsed lists exposed to the view
        public List<int> FilterConditionIds { get; set; } = new List<int>();
        public List<int> FilterSintomaIds { get; set; } = new List<int>();
        public List<int> FilterTratamientoIds { get; set; } = new List<int>();

        // Results
        public int TotalCount { get; set; }
        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();

        // AVAILABLE tags (tags assigned to the current user, shown in UI)
        public List<TagVm> AvailableConditions { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableSintomas { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableTratamientos { get; set; } = new List<TagVm>();

        public class TagVm
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

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
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 9;
            var skip = (PageNumber - 1) * PageSize;

            // Parse incoming comma-separated lists (if any) and expose them to the view
            FilterConditionIds = ParseIds(ConditionIds);
            FilterSintomaIds = ParseIds(SintomaIds);
            FilterTratamientoIds = ParseIds(TratamientoIds);

            // --- Resolve user-associated tags (for UI only) ---
            if (User?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userGuid))
                {
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
            }

            // --- Build baseQuery for contents (apply search if any) ---
            var allowedStatuses = new[] { 1, 2, 3 };

            var baseQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && allowedStatuses.Contains((c.EstadoPublicacion ?? 0)));

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim();
                baseQuery = baseQuery.Where(c =>
                    (c.ContenidoTitulo ?? "").Contains(q) ||
                    (c.ContenidoTextoC ?? "").Contains(q) ||
                    (c.ContenidoTextoL ?? "").Contains(q));
            }

            // --- Build IDs subquery and apply filters (AND across types) ---
            IQueryable<int> idsQuery = baseQuery.Select(c => c.Id);

            if (FilterConditionIds != null && FilterConditionIds.Any())
            {
                var condContentIds = _db.ContenidoCondiciones
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterConditionIds.Contains(rel.CondicionId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => condContentIds.Contains(id));
            }

            if (FilterSintomaIds != null && FilterSintomaIds.Any())
            {
                var sintContentIds = _db.ContenidoSintomas
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterSintomaIds.Contains(rel.SintomaId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => sintContentIds.Contains(id));
            }

            if (FilterTratamientoIds != null && FilterTratamientoIds.Any())
            {
                var tratContentIds = _db.ContenidoTratamientos
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterTratamientoIds.Contains(rel.TratamientoId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => tratContentIds.Contains(id));
            }

            // --- Compute totals and load page of contents ---
            TotalCount = await idsQuery.Distinct().CountAsync();

            var contentsQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => idsQuery.Contains(c.Id))
                .OrderByDescending(c => c.FechaCreado);

            var items = await contentsQuery
                .Skip(skip)
                .Take(PageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Slug = c.ContenidoTituloSlug ?? "",  // ← YA ESTÁ
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal)
                        ? null
                        : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                    // Preferir nombre del perfil cuando exista
                    Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre)) ? c.AutorPerfil.Nombre : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                    AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar)) ? c.AutorPerfil.Avatar : (string)null,
                    AuthorSlug = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.slug)) ? c.AutorPerfil.slug : "",
                    AuthorId = (c.AutorPerfil != null) ? c.AutorPerfil.idUser : (Guid?)null,
                    CreatedAt = c.FechaCreado,
                    Conditions = new List<string>(),
                    Symptoms = new List<string>(),
                    Treatments = new List<string>(),
                    RelatedQuestionsCount = 0
                })
                .ToListAsync();

            Items = items;

            // --- Batch load related metadata for visible items ---
            var contentIds = Items.Select(i => i.Id).ToList();
            if (contentIds.Any())
            {
                // Load conditions
                var conds = await _db.ContenidoCondiciones
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                    .Join(_db.condiciones.AsNoTracking(),
                          rel => rel.CondicionId,
                          c => c.id,
                          (rel, c) => new { rel.ContenidoId, Name = c.nombre })
                    .ToListAsync();

                var condsByContent = conds
                    .GroupBy(x => x.ContenidoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                // Load symptoms
                var snts = await _db.ContenidoSintomas
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                    .Join(_db.sintomas.AsNoTracking(),
                          rel => rel.SintomaId,
                          s => s.id,
                          (rel, s) => new { rel.ContenidoId, Name = s.nombre })
                    .ToListAsync();

                var sntsByContent = snts
                    .GroupBy(x => x.ContenidoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                // Load treatments
                var trts = await _db.ContenidoTratamientos
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                    .Join(_db.tratamientos.AsNoTracking(),
                          rel => rel.TratamientoId,
                          t => t.id,
                          (rel, t) => new { rel.ContenidoId, Name = t.nombre })
                    .ToListAsync();

                var trtsByContent = trts
                    .GroupBy(x => x.ContenidoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                // Load related questions count
                var qCounts = await _db.ContenidosPreguntasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                    .GroupBy(r => r.ContenidoId)
                    .Select(g => new { ContentId = g.Key, Count = g.Select(x => x.PreguntaId).Distinct().Count() })
                    .ToListAsync();

                var qCountsDict = qCounts.ToDictionary(x => x.ContentId, x => x.Count);

                // Attach metadata to items
                foreach (var it in Items)
                {
                    if (condsByContent.TryGetValue(it.Id, out var lc)) it.Conditions = lc;
                    if (sntsByContent.TryGetValue(it.Id, out var ls)) it.Symptoms = ls;
                    if (trtsByContent.TryGetValue(it.Id, out var lt)) it.Treatments = lt;
                    if (qCountsDict.TryGetValue(it.Id, out var qc)) it.RelatedQuestionsCount = qc;
                }

                // --- Load per-content category and build SEO URLs ---
                var catRels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Join(_db.ContenidosCategorias.AsNoTracking(),
                          rel => rel.IdCategoria,
                          cat => cat.Sequence,
                          (rel, cat) => new { rel.IdContenido, cat.Sequence, cat.CategoriaSlug, cat.Nombre, cat.CategoriaPadre })
                    .ToListAsync();

                // Map content ID -> (CategoryName, CategorySlug, CategoryLink)
                var categoryMap = catRels
                    .GroupBy(x => x.IdContenido)
                    .ToDictionary(g => g.Key, g =>
                    {
                        // Prefer child category (has parent) over parent category
                        var chosen = g
                            .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                            .ThenBy(x => x.Sequence)
                            .FirstOrDefault();

                        if (chosen == null)
                            return (Name: (string)null, Slug: (string)null, Link: (string)null);

                        var segment = !string.IsNullOrWhiteSpace(chosen.CategoriaSlug)
                            ? chosen.CategoriaSlug
                            : chosen.Sequence.ToString();

                        // SEO-friendly link: /{categorySlug}
                        var link = $"/{segment}";

                        return (Name: chosen.Nombre, Slug: segment, Link: link);
                    });

                // Attach category data to each item
                foreach (var it in Items)
                {
                    if (categoryMap.TryGetValue(it.Id, out var catInfo))
                    {
                        if (!string.IsNullOrWhiteSpace(catInfo.Name) && !string.IsNullOrWhiteSpace(catInfo.Link))
                        {
                            // Store HTML anchor for rendering in view
                            it.Category = $"<a href=\"{catInfo.Link}\" class=\"blog-category\">{catInfo.Name}</a>";

                            // Store slug for building content URLs
                            it.CategorySlug = catInfo.Slug;
                        }
                    }
                }
            }

            return Page();
        }
    }
}