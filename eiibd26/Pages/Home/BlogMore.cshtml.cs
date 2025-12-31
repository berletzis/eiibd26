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

            var baseQuery = _db.Contenidos.AsNoTracking().Where(c => !c.Eliminado && (c.EstadoPublicacion ?? 0) == 1);

            if (CategorySeq.HasValue)
            {
                var ids = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                    .Where(r => !r.Borrado && r.IdCategoria == CategorySeq.Value)
                    .Select(r => r.IdContenido).Distinct().ToListAsync();
                if (!ids.Any()) { Items = new List<BlogItemVm>(); Response.Headers["X-Total-Count"] = "0"; return Page(); }
                baseQuery = baseQuery.Where(c => ids.Contains(c.Id));
            }
            else if (!string.IsNullOrWhiteSpace(CategorySlug))
            {
                var cat = await _db.ContenidosCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.CategoriaSlug == CategorySlug && !c.Borrado);
                if (cat != null)
                {
                    var ids = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                        .Where(r => !r.Borrado && r.IdCategoria == cat.Sequence)
                        .Select(r => r.IdContenido).Distinct().ToListAsync();
                    if (!ids.Any()) { Items = new List<BlogItemVm>(); Response.Headers["X-Total-Count"] = "0"; return Page(); }
                    baseQuery = baseQuery.Where(c => ids.Contains(c.Id));
                }
            }

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var q = Q.Trim();
                baseQuery = baseQuery.Where(c => (c.ContenidoTitulo ?? "").Contains(q) || (c.ContenidoTextoC ?? "").Contains(q) || (c.ContenidoTextoL ?? "").Contains(q));
            }

            var condIds = ParseIds(ConditionIds);
            var sintIds = ParseIds(SintomaIds);
            var tratIds = ParseIds(TratamientoIds);

            IQueryable<int> idsQuery = baseQuery.Select(c => c.Id);

            if (condIds.Any())
            {
                var condContentIds = _db.ContenidoCondiciones.AsNoTracking().Where(rel => !rel.Borrado && condIds.Contains(rel.CondicionId)).Select(rel => rel.ContenidoId).Distinct();
                idsQuery = idsQuery.Where(id => condContentIds.Contains(id));
            }
            if (sintIds.Any())
            {
                var sintContentIds = _db.ContenidoSintomas.AsNoTracking().Where(rel => !rel.Borrado && sintIds.Contains(rel.SintomaId)).Select(rel => rel.ContenidoId).Distinct();
                idsQuery = idsQuery.Where(id => sintContentIds.Contains(id));
            }
            if (tratIds.Any())
            {
                var tratContentIds = _db.ContenidoTratamientos.AsNoTracking().Where(rel => !rel.Borrado && tratIds.Contains(rel.TratamientoId)).Select(rel => rel.ContenidoId).Distinct();
                idsQuery = idsQuery.Where(id => tratContentIds.Contains(id));
            }

            var total = await idsQuery.Distinct().CountAsync();
            Response.Headers["X-Total-Count"] = total.ToString();

            var contentsQuery = _db.Contenidos.AsNoTracking().Where(c => idsQuery.Contains(c.Id)).OrderByDescending(c => c.FechaCreado);

            var items = await contentsQuery.Skip(skip).Take(PageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal) ? null : ("/uploads/contenidos/" + c.URLImagenPrincipal),
                    Author = string.IsNullOrEmpty(c.Autor) ? "Autor" : c.Autor,
                    CreatedAt = c.FechaCreado
                }).ToListAsync();

            Items = items;
            return Page(); // returns fragment rendering _BlogItems partial
        }
    }
}