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

        [BindProperty(SupportsGet = true)] public string Q { get; set; }
        [BindProperty(SupportsGet = true)] public string ConditionIds { get; set; }
        [BindProperty(SupportsGet = true)] public string SintomaIds { get; set; }
        [BindProperty(SupportsGet = true)] public string TratamientoIds { get; set; }

        [BindProperty(SupportsGet = true)] public int? CategorySeq { get; set; }
        [BindProperty(SupportsGet = true)] public string CategorySlug { get; set; }

        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();

        private static List<int> ParseIds(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => { int.TryParse(s.Trim(), out var v); return v; })
                      .Where(v => v > 0).Distinct().ToList();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 3;
            var skip = (PageNumber - 1) * PageSize;

            // default allowed statuses for content lists: 1,2,3
            var allowedStatuses = new[] { 1, 2, 3 };

            var baseQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && allowedStatuses.Contains((c.EstadoPublicacion ?? 0)));

            // Filtro por categoría (secuencia o slug)
            if (CategorySeq.HasValue)
            {
                var ids = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria == CategorySeq.Value)
                    .Select(r => r.IdContenido)
                    .Distinct()
                    .ToListAsync();

                if (!ids.Any())
                {
                    Items = new List<BlogItemVm>();
                    Response.Headers["X-Total-Count"] = "0";
                    return Page();
                }

                baseQuery = baseQuery.Where(c => ids.Contains(c.Id));
            }
            else if (!string.IsNullOrWhiteSpace(CategorySlug))
            {
                // Case-insensitive comparison for slug
                var normalizedSlug = CategorySlug.ToLowerInvariant();
                var cat = await _db.ContenidosCategorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CategoriaSlug.ToLower() == normalizedSlug && !c.Borrado);

                if (cat != null)
                {
                    var ids = await _db.ContenidosCategoriasRelacion
                        .AsNoTracking()
                        .Where(r => !r.Borrado && r.IdCategoria == cat.Sequence)
                        .Select(r => r.IdContenido)
                        .Distinct()
                        .ToListAsync();

                    if (!ids.Any())
                    {
                        Items = new List<BlogItemVm>();
                        Response.Headers["X-Total-Count"] = "0";
                        return Page();
                    }

                    baseQuery = baseQuery.Where(c => ids.Contains(c.Id));
                }
            }

            // Búsqueda de texto
            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                baseQuery = baseQuery.Where(c =>
                    (c.ContenidoTitulo ?? "").Contains(q) ||
                    (c.ContenidoTextoC ?? "").Contains(q) ||
                    (c.ContenidoTextoL ?? "").Contains(q));
            }

            // Filtros por tags
            var condIds = ParseIds(ConditionIds);
            var sintIds = ParseIds(SintomaIds);
            var tratIds = ParseIds(TratamientoIds);

            IQueryable<int> idsQuery = baseQuery.Select(c => c.Id);

            if (condIds.Any())
            {
                var condContentIds = _db.ContenidoCondiciones
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && condIds.Contains(rel.CondicionId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();

                idsQuery = idsQuery.Where(id => condContentIds.Contains(id));
            }

            if (sintIds.Any())
            {
                var sintContentIds = _db.ContenidoSintomas
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && sintIds.Contains(rel.SintomaId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();

                idsQuery = idsQuery.Where(id => sintContentIds.Contains(id));
            }

            if (tratIds.Any())
            {
                var tratContentIds = _db.ContenidoTratamientos
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && tratIds.Contains(rel.TratamientoId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();

                idsQuery = idsQuery.Where(id => tratContentIds.Contains(id));
            }

            var total = await idsQuery.Distinct().CountAsync();
            Response.Headers["X-Total-Count"] = total.ToString();

            // ✅ LOAD ALL RESULTS IN MEMORY FOR SCORING (before pagination)
            var contentsQueryAll = await _db.Contenidos
                .AsNoTracking()
                .Include(c => c.AutorPerfil)
                .Where(c => idsQuery.Contains(c.Id))
                .ToListAsync();  // ← Load all into memory for scoring

            // ✅ APPLY RELEVANCE SCORING when searching
            bool hasSearch = !string.IsNullOrWhiteSpace(Q);
            if (hasSearch)
            {
                var contentWithScores = contentsQueryAll
                    .Select(c => new
                    {
                        Content = c,
                        RelevanceScore = CalculateRelevanceScore(c, Q)
                    })
                    .OrderByDescending(x => x.RelevanceScore)           // ← Primary: relevance score
                    .ThenByDescending(x => x.Content.FechaCreado)       // ← Secondary: date
                    .ToList();

                contentsQueryAll = contentWithScores.Select(x => x.Content).ToList();

                // DEBUG
                var debugResults = contentWithScores.Take(5).Select(c => new { Title = c.Content.ContenidoTitulo, Score = c.RelevanceScore }).ToList();
                System.Diagnostics.Debug.WriteLine($"🔍 SEARCH: '{Q}' | Found {contentsQueryAll.Count} results | PAGE {PageNumber}");
                foreach (var r in debugResults)
                {
                    System.Diagnostics.Debug.WriteLine($"  → Score {r.Score}: {r.Title}");
                }
            }
            else
            {
                contentsQueryAll = contentsQueryAll.OrderByDescending(c => c.FechaCreado).ToList();
            }

            var contentsQuery = contentsQueryAll.AsEnumerable();

            var items = contentsQuery
                .Skip(skip)
                .Take(PageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Slug = c.ContenidoTituloSlug ?? "",
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal)
                        ? null
                        : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                    // Prefer profile display name when available, fallback to the raw Autor field
                    Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre))
                        ? c.AutorPerfil.Nombre
                        : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                    // Attempt to resolve author avatar and profile identifiers from related Perfil if available
                    AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar))
                        ? c.AutorPerfil.Avatar
                        : (string)null,
                    AuthorSlug = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.slug)) ? c.AutorPerfil.slug : "",
                    AuthorId = (c.AutorPerfil != null) ? c.AutorPerfil.idUser : (Guid?)null,
                    CreatedAt = c.FechaCreado,
                    Conditions = new List<string>(),
                    Symptoms = new List<string>(),
                    Treatments = new List<string>(),
                    RelatedQuestionsCount = 0
                })
                .ToList();

            // Adjuntar categorías y slugs igual que en Index/porCategoria
            var contentIds = items.Select(i => i.Id).ToList();
            if (contentIds.Any())
            {
                var catRels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => contentIds.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Join(_db.ContenidosCategorias.AsNoTracking(),
                          rel => rel.IdCategoria,
                          cat => cat.Sequence,
                          (rel, cat) => new
                          {
                              rel.IdContenido,
                              rel.EsPrincipal,
                              cat.Sequence,
                              cat.CategoriaSlug,
                              cat.Nombre,
                              cat.CategoriaPadre
                          })
                    .ToListAsync();

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
                        });

                foreach (var it in items)
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

            Items = items;
            return Page();
        }

        /// <summary>
        /// Calculates relevance score for content based on where the search term appears.
        /// ✅ SCORING RULES:
        /// 
        /// BÚSQUEDA EN TÍTULO (Prioridad 1):
        ///   - Exacto (título == término)                           → 10,000
        ///   - Comienza con (término al inicio)                    → 5,000
        ///   - Contiene palabra límite (con espacios alrededor)    → 2,000
        ///   - Contiene como substring (en cualquier lugar)        → 1,000
        /// 
        /// BÚSQUEDA EN CONTENIDO LARGO (Prioridad 2, solo si NO en título):
        ///   - Contiene término                                     → 100
        /// 
        /// GARANTÍA: Resultados en TÍTULO siempre > Resultados en CONTENIDO
        /// </summary>
        private int CalculateRelevanceScore(Contenido content, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return 0;

            int score = 0;
            string term = searchTerm.Trim().ToLower();
            bool foundInTitle = false;

            // =====================================================
            // PASO 1: BUSCAR EN TÍTULO (máxima prioridad)
            // =====================================================
            if (!string.IsNullOrWhiteSpace(content.ContenidoTitulo))
            {
                string titleLower = content.ContenidoTitulo.ToLower();

                // 🥇 EXACTO: Título = término exacto
                if (titleLower == term)
                {
                    score = 10000;
                    foundInTitle = true;
                    System.Diagnostics.Debug.WriteLine($"  [EXACTO] '{term}' = '{content.ContenidoTitulo}' → {score}");
                }
                // 🥈 COMIENZA CON: Término al inicio del título
                else if (titleLower.StartsWith(term + " "))
                {
                    score = 5000;
                    foundInTitle = true;
                    System.Diagnostics.Debug.WriteLine($"  [COMIENZA] '{term}' at start of '{content.ContenidoTitulo}' → {score}");
                }
                // 🥉 PALABRA LÍMITE: Término con espacios alrededor
                else if (titleLower.Contains(" " + term + " ") ||
                         titleLower.Contains(" " + term) ||
                         titleLower.Contains(term + " "))
                {
                    score = 2000;
                    foundInTitle = true;
                    System.Diagnostics.Debug.WriteLine($"  [LÍMITE] '{term}' word boundary in '{content.ContenidoTitulo}' → {score}");
                }
                // 🎯 SUBSTRING: Contiene el término
                else if (titleLower.Contains(term))
                {
                    score = 1000;
                    foundInTitle = true;
                    System.Diagnostics.Debug.WriteLine($"  [SUBSTRING] '{term}' in '{content.ContenidoTitulo}' → {score}");
                }
            }

            // =====================================================
            // PASO 2: BUSCAR EN CONTENIDO LARGO (solo si NOT en título)
            // =====================================================
            if (!foundInTitle && !string.IsNullOrWhiteSpace(content.ContenidoTextoL))
            {
                string longLower = content.ContenidoTextoL.ToLower();
                if (longLower.Contains(term))
                {
                    score = 100;
                    System.Diagnostics.Debug.WriteLine($"  [LARGO] '{term}' in long content → {score}");
                }
            }

            return score;
        }
    }
}