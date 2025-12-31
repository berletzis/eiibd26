using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        public int PageSize { get; set; } = 7;

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

        // AVAILABLE tags (derived from contents) — used in header to filter
        public List<TagVm> AvailableConditions { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableSintomas { get; set; } = new List<TagVm>();
        public List<TagVm> AvailableTratamientos { get; set; } = new List<TagVm>();

        public class TagVm { public int Id { get; set; } public string Name { get; set; } = ""; }

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
            if (PageSize < 1) PageSize = 7;
            var skip = (PageNumber - 1) * PageSize;

            // parse incoming comma-separated lists (if any) and expose them to the view
            FilterConditionIds = ParseIds(ConditionIds);
            FilterSintomaIds = ParseIds(SintomaIds);
            FilterTratamientoIds = ParseIds(TratamientoIds);

            // Build baseQuery (published contents), apply search if provided
            var baseQuery = _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1);

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim();
                baseQuery = baseQuery.Where(c => (c.ContenidoTitulo ?? "").Contains(q) || (c.ContenidoTextoC ?? "").Contains(q) || (c.ContenidoTextoL ?? "").Contains(q));
            }

            // Build IDs subquery and apply filters (OR within types, AND across types)
            IQueryable<int> idsQuery = baseQuery.Select(c => c.Id);

            if (FilterConditionIds.Any())
            {
                var condContentIds = _db.ContenidoCondiciones
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterConditionIds.Contains(rel.CondicionId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => condContentIds.Contains(id));
            }

            if (FilterSintomaIds.Any())
            {
                var sintContentIds = _db.ContenidoSintomas
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterSintomaIds.Contains(rel.SintomaId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => sintContentIds.Contains(id));
            }

            if (FilterTratamientoIds.Any())
            {
                var tratContentIds = _db.ContenidoTratamientos
                    .AsNoTracking()
                    .Where(rel => !rel.Borrado && FilterTratamientoIds.Contains(rel.TratamientoId))
                    .Select(rel => rel.ContenidoId)
                    .Distinct();
                idsQuery = idsQuery.Where(id => tratContentIds.Contains(id));
            }

            // Compute total and page results
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

            Items = items;

            // Batch load related metadata for the items shown
            var contentIds = Items.Select(i => i.Id).ToList();
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

                foreach (var it in Items)
                {
                    if (condsByContent.TryGetValue(it.Id, out var lc)) it.Conditions = lc;
                    if (sntsByContent.TryGetValue(it.Id, out var ls)) it.Symptoms = ls;
                    if (trtsByContent.TryGetValue(it.Id, out var lt)) it.Treatments = lt;
                    if (qCountsDict.TryGetValue(it.Id, out var qc)) it.RelatedQuestionsCount = qc;
                }
            }

            // --- AVAILABLE TAGS (derived from published contents globally) ---
            // We get distinct condition/sintoma/tratamiento values associated to published contents (optionally limited)
            var availableCond = await _db.ContenidoCondiciones
                .AsNoTracking()
                .Where(rel => !rel.Borrado)
                .Join(_db.Contenidos.AsNoTracking().Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1),
                      rel => rel.ContenidoId, c => c.Id, (rel, c) => rel)
                .Join(_db.condiciones.AsNoTracking(), rel => rel.CondicionId, cond => cond.id, (rel, cond) => new { Id = cond.id, Name = cond.nombre })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            AvailableConditions = availableCond.Select(x => new TagVm { Id = x.Id, Name = x.Name }).ToList();

            var availableSint = await _db.ContenidoSintomas
                .AsNoTracking()
                .Where(rel => !rel.Borrado)
                .Join(_db.Contenidos.AsNoTracking().Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1),
                      rel => rel.ContenidoId, c => c.Id, (rel, c) => rel)
                .Join(_db.sintomas.AsNoTracking(), rel => rel.SintomaId, s => s.id, (rel, s) => new { Id = s.id, Name = s.nombre })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            AvailableSintomas = availableSint.Select(x => new TagVm { Id = x.Id, Name = x.Name }).ToList();

            var availableTrat = await _db.ContenidoTratamientos
                .AsNoTracking()
                .Where(rel => !rel.Borrado)
                .Join(_db.Contenidos.AsNoTracking().Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1),
                      rel => rel.ContenidoId, c => c.Id, (rel, c) => rel)
                .Join(_db.tratamientos.AsNoTracking(), rel => rel.TratamientoId, t => t.id, (rel, t) => new { Id = t.id, Name = t.nombre })
                .Distinct()
                .OrderBy(x => x.Name)
                .ToListAsync();

            AvailableTratamientos = availableTrat.Select(x => new TagVm { Id = x.Id, Name = x.Name }).ToList();

            return Page();
        }
    }
}