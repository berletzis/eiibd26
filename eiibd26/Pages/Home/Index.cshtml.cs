using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace eiibd26.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public IndexModel(ApplicationDbContext db, IMemoryCache cache) 
        { 
            _db = db;
            _cache = cache;
        }

        public HeroViewModel Hero { get; set; } = new HeroViewModel();
        public BlogListViewModel BlogList { get; set; } = new BlogListViewModel();

        // New: Top lists for specific category sequences
        public List<BlogItemVm> Featured1042 { get; set; } = new List<BlogItemVm>();
        public List<BlogItemVm> Featured1043 { get; set; } = new List<BlogItemVm>();

        public async Task OnGetAsync()
        {
            // Hero placeholder
            Hero.Title = "#HazViralLoQueImporta";
            Hero.Subtitle = "Analizamos datos relacionados la Enfermedad Inflamatoria Intestinal";
            Hero.CallToAction = "Registrate!";

            const int pageSize = 7;

            // ===== PERFORMANCE: Cache main blog list for 3 minutes =====
            var cacheKey = "home_blog_list_v1";
            BlogList = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3);
                entry.SlidingExpiration = TimeSpan.FromMinutes(1);
                entry.SetPriority(CacheItemPriority.High);

                var list = new BlogListViewModel
                {
                    PageSize = pageSize,
                    PageNumber = 1
                };

                list.TotalCount = await _db.Contenidos
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
                        Slug = c.ContenidoTituloSlug ?? "",
                        Excerpt = c.ContenidoTextoC ?? "",
                        ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                            // Prefer profile name when available
                            Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre)) ? c.AutorPerfil.Nombre : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                            AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar))
                        ? c.AutorPerfil.Avatar
                        : (string)null,
                        CreatedAt = c.FechaCreado,
                        Conditions = new List<string>(),
                        Symptoms = new List<string>(),
                        Treatments = new List<string>(),
                        RelatedQuestionsCount = 0
                    })
                    .ToListAsync();

                list.Items = items;

                // Batch load related metadata for the items shown (so home initial page shows metadata)
                var contentIds = list.Items.Select(i => i.Id).ToList();
                if (contentIds.Any())
                {
                    var conds = await _db.ContenidoCondiciones
                        .AsNoTracking()
                        .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                        .Join(_db.condiciones.AsNoTracking(), rel => rel.CondicionId, c => c.id, (rel, c) => new { rel.ContenidoId, Name = c.nombre })
                        .ToListAsync();

                    var condsByContent = conds.GroupBy(x => x.ContenidoId).ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                    var snts = await _db.ContenidoSintomas
                        .AsNoTracking()
                        .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                        .Join(_db.sintomas.AsNoTracking(), rel => rel.SintomaId, s => s.id, (rel, s) => new { rel.ContenidoId, Name = s.nombre })
                        .ToListAsync();

                    var sntsByContent = snts.GroupBy(x => x.ContenidoId).ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                    var trts = await _db.ContenidoTratamientos
                        .AsNoTracking()
                        .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                        .Join(_db.tratamientos.AsNoTracking(), rel => rel.TratamientoId, t => t.id, (rel, t) => new { rel.ContenidoId, Name = t.nombre })
                        .ToListAsync();

                    var trtsByContent = trts.GroupBy(x => x.ContenidoId).ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct().ToList());

                    var qCounts = await _db.ContenidosPreguntasRelacion
                        .AsNoTracking()
                        .Where(r => contentIds.Contains(r.ContenidoId) && !r.Borrado)
                        .GroupBy(r => r.ContenidoId)
                        .Select(g => new { ContentId = g.Key, Count = g.Select(x => x.PreguntaId).Distinct().Count() })
                        .ToListAsync();

                    var qCountsDict = qCounts.ToDictionary(x => x.ContentId, x => x.Count);

                    foreach (var it in list.Items)
                    {
                        if (condsByContent.TryGetValue(it.Id, out var lc)) it.Conditions = lc;
                        if (sntsByContent.TryGetValue(it.Id, out var ls)) it.Symptoms = ls;
                        if (trtsByContent.TryGetValue(it.Id, out var lt)) it.Treatments = lt;
                        if (qCountsDict.TryGetValue(it.Id, out var qc)) it.RelatedQuestionsCount = qc;
                    }
                }

                return list;
            });

            // ===== PERFORMANCE: Cache featured content for 5 minutes =====
            async Task<List<BlogItemVm>> GetTopForEstadoAsync(int estadoPublicacion)
            {
                var cacheKey = $"home_featured_estado_{estadoPublicacion}_v1";
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                    entry.SetPriority(CacheItemPriority.Normal);

                    var list = await _db.Contenidos
                        .AsNoTracking()
                        .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == estadoPublicacion)
                        .OrderByDescending(c => c.FechaCreado)
                        .Take(3)
                        .Select(c => new BlogItemVm
                        {
                            Id = c.Id,
                            Title = c.ContenidoTitulo ?? "",
                            Slug = c.ContenidoTituloSlug ?? "",
                            Excerpt = c.ContenidoTextoC ?? "",
                            ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                            // Prefer profile name when available
                            Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre)) ? c.AutorPerfil.Nombre : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                            CreatedAt = c.FechaCreado,
                            AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar)) ? c.AutorPerfil.Avatar : (string)null,
                            AuthorSlug = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.slug)) ? c.AutorPerfil.slug : "",
                            AuthorId = (c.AutorPerfil != null) ? c.AutorPerfil.idUser : (Guid?)null,
                            Conditions = new List<string>(),
                            Symptoms = new List<string>(),
                            Treatments = new List<string>(),
                            RelatedQuestionsCount = 0
                        })
                        .ToListAsync();

                    return list;
                });
            }

            Featured1042 = await GetTopForEstadoAsync(2);
            Featured1043 = await GetTopForEstadoAsync(3);

            async Task AttachCatsAsync(List<BlogItemVm> list)
            {
                if (list == null || !list.Any()) return;

                var ids = list.Select(x => x.Id).ToList();

                var catRels = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => ids.Contains(r.IdContenido) && !r.Borrado && r.IdCategoria != null)
                    .Join(_db.ContenidosCategorias.AsNoTracking(),
                          rel => rel.IdCategoria,
                          cat => cat.Sequence,
                          (rel, cat) => new {
                              rel.IdContenido,
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
                            var chosen = g
                                .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                                .ThenBy(x => x.Sequence)
                                .FirstOrDefault();

                            if (chosen == null)
                                return (Name: (string)null, Slug: (string)null, Id: (int?)null);

                            var segment = !string.IsNullOrWhiteSpace(chosen.CategoriaSlug)
                                ? chosen.CategoriaSlug
                                : chosen.Sequence.ToString();

                            // IMPORTANTE: Devolver 3 elementos (Name, Slug, Id)
                            return (Name: chosen.Nombre, Slug: segment, Id: (int?)chosen.Sequence);
                        }
                    );

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

            await AttachCatsAsync(Featured1042);
            await AttachCatsAsync(Featured1043);
        }
    }
}