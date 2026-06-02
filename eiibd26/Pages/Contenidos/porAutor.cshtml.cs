using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models;
using System.Collections.Generic;
using System;
using eiibd26.Services.Validacion;
using eiibd26.Models.Validacion;

namespace eiibd26.Pages.Contenidos
{
    public class PorAutorModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IValidacionContenidoService _validacionSvc;
        public PorAutorModel(ApplicationDbContext db, IValidacionContenidoService validacionSvc)
        {
            _db = db;
            _validacionSvc = validacionSvc;
        }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 9;

        public string AutorNombre { get; set; } = "";
        public string AutorAvatar { get; set; } = "";
        public string AutorSlug { get; set; } = "";
        public List<BlogItemVm> Items { get; set; } = new List<BlogItemVm>();
        public int TotalCount { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 9;

            var autor = await _db.Perfil
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.slug == slug);

            if (autor == null) return NotFound();

            AutorNombre = autor.Nombre ?? "Autor";
            AutorAvatar = autor.Avatar ?? "";
            AutorSlug = autor.slug ?? "";

            (TotalCount, Items) = await QueryItemsAsync(autor.idUser, PageNumber, PageSize);
            await PopularCategoriasAsync(Items);

            // Batch-load médicos validadores (1 query set for all items)
            var ids = Items.Select(i => i.Id).ToList();
            var validadoresDict = await _validacionSvc.ObtenerValidadoresPorContenidosAsync(
                TipoContenidoValidado.Articulo, ids);
            foreach (var it in Items)
                if (validadoresDict.TryGetValue(it.Id, out var v))
                    it.Validadores = v;

            return Page();
        }

        public async Task<IActionResult> OnGetItemsAsync(string slug, int pageNumber, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 9;

            var autor = await _db.Perfil.AsNoTracking()
                .FirstOrDefaultAsync(p => p.slug == slug);
            if (autor == null) return NotFound();

            var (total, items) = await QueryItemsAsync(autor.idUser, pageNumber, pageSize);
            await PopularCategoriasAsync(items);

            var ajaxIds = items.Select(i => i.Id).ToList();
            var ajaxValidadores = await _validacionSvc.ObtenerValidadoresPorContenidosAsync(
                TipoContenidoValidado.Articulo, ajaxIds);
            foreach (var it in items)
                if (ajaxValidadores.TryGetValue(it.Id, out var v))
                    it.Validadores = v;

            Response.Headers["X-Total-Count"] = total.ToString();
            return Partial("~/Pages/Shared/_BlogItems.cshtml", (IEnumerable<BlogItemVm>)items);
        }

        private async Task<(int total, List<BlogItemVm> items)> QueryItemsAsync(Guid autorId, int pageNumber, int pageSize)
        {
            var allowedStatuses = new[] { 1, 2, 3 };

            var total = await _db.Contenidos
                .AsNoTracking()
                .Where(c => !c.Eliminado &&
                           allowedStatuses.Contains(c.EstadoPublicacion ?? 0) &&
                           c.IdAutor == autorId)
                .CountAsync();

            var items = await _db.Contenidos
                .AsNoTracking()
                .Include(c => c.AutorPerfil)
                .Where(c => !c.Eliminado &&
                           allowedStatuses.Contains(c.EstadoPublicacion ?? 0) &&
                           c.IdAutor == autorId)
                .OrderByDescending(c => c.FechaCreado)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new BlogItemVm
                {
                    Id = c.Id,
                    Title = c.ContenidoTitulo ?? "",
                    Slug = c.ContenidoTituloSlug ?? "",
                    Excerpt = c.ContenidoTextoC ?? "",
                    ImageUrl = string.IsNullOrEmpty(c.URLImagenPrincipal)
                        ? null
                        : "/uploads/contenidos/" + c.URLImagenPrincipal,
                    Author = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Nombre))
                        ? c.AutorPerfil.Nombre
                        : (string.IsNullOrWhiteSpace(c.Autor) ? "Autor" : c.Autor),
                    AuthorImageUrl = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.Avatar))
                        ? c.AutorPerfil.Avatar
                        : (string)null,
                    AuthorSlug = (c.AutorPerfil != null && !string.IsNullOrWhiteSpace(c.AutorPerfil.slug))
                        ? c.AutorPerfil.slug
                        : "",
                    AuthorId = (c.AutorPerfil != null) ? c.AutorPerfil.idUser : (Guid?)null,
                    CreatedAt = c.FechaCreado
                })
                .ToListAsync();

            return (total, items);
        }

        private async Task PopularCategoriasAsync(List<BlogItemVm> items)
        {
            foreach (var item in items)
            {
                var relCatIds = await _db.ContenidosCategoriasRelacion.AsNoTracking()
                    .Where(r => r.IdContenido == item.Id && !r.Borrado && r.IdCategoria != null)
                    .Select(r => r.IdCategoria!.Value)
                    .Distinct().ToListAsync();

                if (!relCatIds.Any()) continue;

                var primaryCat = await _db.ContenidosCategorias.AsNoTracking()
                    .Where(c => relCatIds.Contains(c.Sequence) && !c.Borrado)
                    .OrderBy(c => c.CategoriaPadre.HasValue ? 0 : 1)
                    .ThenBy(c => c.Sequence)
                    .FirstOrDefaultAsync();

                if (primaryCat != null)
                    item.PrimaryCategorySlug = !string.IsNullOrWhiteSpace(primaryCat.CategoriaSlug)
                        ? primaryCat.CategoriaSlug
                        : primaryCat.Sequence.ToString();
            }
        }
    }
}
