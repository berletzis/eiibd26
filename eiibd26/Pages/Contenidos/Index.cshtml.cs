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
using eiibd26.Domain.Constants;

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

        [BindProperty(Name = "q", SupportsGet = true)]
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
            PageSize = Math.Min(PageSize, 50);
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
            var baseQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && EstadoPublicacion.Visibles.Contains((c.EstadoPublicacion ?? 0)));

            // ✅ SEARCH: title and short description only (no table scan on HTML body)
            bool hasSearch = !string.IsNullOrWhiteSpace(SearchQuery);
            string searchTerm = hasSearch ? SearchQuery.Trim() : "";

            if (hasSearch)
            {
                baseQuery = baseQuery.Where(c =>
                    (c.ContenidoTitulo ?? "").Contains(searchTerm) ||
                    (c.ContenidoTextoC ?? "").Contains(searchTerm));
            }

            // --- Build IDs subquery and apply filters (OR across all types) ---
            IQueryable<int> idsQuery = baseQuery.Select(c => c.Id);

            // Si hay filtros, usar OR para combinar resultados de diferentes tipos
            if ((FilterConditionIds != null && FilterConditionIds.Any()) ||
                (FilterSintomaIds != null && FilterSintomaIds.Any()) ||
                (FilterTratamientoIds != null && FilterTratamientoIds.Any()))
            {
                // Crear un conjunto de IDs que cumplan con CUALQUIER filtro (OR)
                var filteredIds = new HashSet<int>();

                // Agregar IDs de contenidos que coincidan con Condiciones seleccionadas
                if (FilterConditionIds != null && FilterConditionIds.Any())
                {
                    var condContentIds = await _db.ContenidoCondiciones
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado && FilterConditionIds.Contains(rel.CondicionId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var id in condContentIds)
                        filteredIds.Add(id);
                }

                // Agregar IDs de contenidos que coincidan con Síntomas seleccionados
                if (FilterSintomaIds != null && FilterSintomaIds.Any())
                {
                    var sintContentIds = await _db.ContenidoSintomas
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado && FilterSintomaIds.Contains(rel.SintomaId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var id in sintContentIds)
                        filteredIds.Add(id);
                }

                // Agregar IDs de contenidos que coincidan con Tratamientos seleccionados
                if (FilterTratamientoIds != null && FilterTratamientoIds.Any())
                {
                    var tratContentIds = await _db.ContenidoTratamientos
                        .AsNoTracking()
                        .Where(rel => !rel.Borrado && FilterTratamientoIds.Contains(rel.TratamientoId))
                        .Select(rel => rel.ContenidoId)
                        .Distinct()
                        .ToListAsync();

                    foreach (var id in tratContentIds)
                        filteredIds.Add(id);
                }

                // Aplicar el filtro: solo incluir contenidos que están en el conjunto filteredIds
                idsQuery = idsQuery.Where(id => filteredIds.Contains(id));
            }

            // --- Compute totals and load page of contents ---
            TotalCount = await idsQuery.Distinct().CountAsync();

            var contentsQuery = _db.Contenidos
                .AsNoTracking()
                .Include(c => c.AutorPerfil)
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
                        : (c.URLImagenPrincipal.StartsWith("http") || c.URLImagenPrincipal.StartsWith("/")
                            ? c.URLImagenPrincipal
                            : "/uploads/contenidos/" + c.URLImagenPrincipal),
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
                          (rel, cat) => new { rel.IdContenido, rel.EsPrincipal, cat.Sequence, cat.CategoriaSlug, cat.Nombre, cat.CategoriaPadre })
                    .ToListAsync();

                // Map content ID -> (CategoryName, CategorySlug, CategoryLink)
                var categoryMap = catRels
                    .GroupBy(x => x.IdContenido)
                    .ToDictionary(g => g.Key, g =>
                    {
                        // First try to find primary category
                        var primary = g.FirstOrDefault(x => x.EsPrincipal == true);
                        if (primary != null)
                        {
                            var segment = !string.IsNullOrWhiteSpace(primary.CategoriaSlug)
                                ? primary.CategoriaSlug
                                : primary.Sequence.ToString();
                            var link = $"/{segment}";
                            return (Name: primary.Nombre, Slug: segment, Link: link);
                        }

                        // Otherwise, prefer child category (has parent) over parent category
                        var chosen = g
                            .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                            .ThenBy(x => x.Sequence)
                            .FirstOrDefault();

                        if (chosen == null)
                            return (Name: (string)null, Slug: (string)null, Link: (string)null);

                        var seg = !string.IsNullOrWhiteSpace(chosen.CategoriaSlug)
                            ? chosen.CategoriaSlug
                            : chosen.Sequence.ToString();

                        // SEO-friendly link: /{categorySlug}
                        var lnk = $"/{seg}";

                        return (Name: chosen.Nombre, Slug: seg, Link: lnk);
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