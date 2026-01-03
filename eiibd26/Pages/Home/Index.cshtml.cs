// File: Pages/Home/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;

namespace eiibd26.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) { _db = db; }

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
            BlogList.PageSize = pageSize;
            BlogList.PageNumber = 1;

            BlogList.TotalCount = await _db.Contenidos
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
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                    Author = string.IsNullOrEmpty(c.Autor) ? "Autor" : c.Autor,
                    CreatedAt = c.FechaCreado,
                    Conditions = new List<string>(),
                    Symptoms = new List<string>(),
                    Treatments = new List<string>(),
                    RelatedQuestionsCount = 0
                })
                .ToListAsync();

            BlogList.Items = items;

            // Batch load related metadata for the items shown (so home initial page shows metadata)
            var contentIds = BlogList.Items.Select(i => i.Id).ToList();
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

                foreach (var it in BlogList.Items)
                {
                    if (condsByContent.TryGetValue(it.Id, out var lc)) it.Conditions = lc;
                    if (sntsByContent.TryGetValue(it.Id, out var ls)) it.Symptoms = ls;
                    if (trtsByContent.TryGetValue(it.Id, out var lt)) it.Treatments = lt;
                    if (qCountsDict.TryGetValue(it.Id, out var qc)) it.RelatedQuestionsCount = qc;
                }
            }

            // --- New: featured rows for categories 1042 and 1043 ---
            // Helper to get top N items for a category sequence
            async Task<List<BlogItemVm>> GetTopForCategoryAsync(int categorySeq, int topN)
            {
                var distinctIds = await _db.ContenidosCategoriasRelacion
                    .AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria == categorySeq)
                    .Select(r => r.IdContenido)
                    .Distinct()
                    .ToListAsync();

                if (!distinctIds.Any()) return new List<BlogItemVm>();

                var list = await _db.Contenidos
                    .AsNoTracking()
                    .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1 && distinctIds.Contains(c.Id))
                    .OrderByDescending(c => c.FechaCreado)
                    .Take(topN)
                    .Select(c => new BlogItemVm
                    {
                        Id = c.Id,
                        Title = c.ContenidoTitulo ?? "",
                        Excerpt = c.ContenidoTextoC ?? "",
                        ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                        Author = string.IsNullOrEmpty(c.Autor) ? "Autor" : c.Autor,
                        CreatedAt = c.FechaCreado
                    })
                    .ToListAsync();

                return list;
            }

            Featured1042 = await GetTopForCategoryAsync(1042, 3);
            Featured1043 = await GetTopForCategoryAsync(1043, 3);
        }
    }
}