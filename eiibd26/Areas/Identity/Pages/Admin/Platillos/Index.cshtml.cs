using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Admin.Platillos
{
    [Authorize(Roles = "Administrador")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)] public string? Q { get; set; }
        [BindProperty(SupportsGet = true)] public int? CategoriaId { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;
        [BindProperty(SupportsGet = true)] public bool MostrarInactivos { get; set; }

        public List<Row> Items { get; set; } = new();
        public List<PlatCategoria> Categorias { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public class Row
        {
            public int Id { get; set; }
            public string Codigo { get; set; } = "";
            public string Nombre { get; set; } = "";
            public string? Categoria { get; set; }
            public int NumIngredientes { get; set; }
            public bool Activo { get; set; }
        }

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 20;
            PageSize = System.Math.Min(PageSize, 50);

            // Combo de categoría: solo activas para filtrar registros nuevos, pero si el filtro
            // apunta a una inactiva la incluimos para que la selección se muestre (§7.2).
            Categorias = await _db.PlatCategorias.AsNoTracking()
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                .ToListAsync();
            if (CategoriaId.HasValue && !Categorias.Any(c => c.Id == CategoriaId.Value))
            {
                var extra = await _db.PlatCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == CategoriaId.Value);
                if (extra != null) Categorias.Add(extra);
            }

            var query = _db.PlatPlatillos.AsNoTracking().AsQueryable();

            if (!MostrarInactivos)
                query = query.Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var term = Q.Trim();
                query = query.Where(p => p.Codigo.Contains(term) || p.Nombre.Contains(term));
            }

            if (CategoriaId.HasValue)
                query = query.Where(p => p.CategoriaId == CategoriaId.Value);

            TotalCount = await query.CountAsync();
            TotalPages = TotalCount == 0 ? 1 : (int)System.Math.Ceiling(TotalCount / (double)PageSize);
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            Items = await query
                .OrderBy(p => p.Codigo)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new Row
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                    NumIngredientes = p.Ingredientes.Count,
                    Activo = p.Activo
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleActivoAsync(int id, string? q, int? categoriaId, int pageNumber, bool mostrarInactivos)
        {
            var ent = await _db.PlatPlatillos.FirstOrDefaultAsync(x => x.Id == id);
            if (ent == null) { ErrorMessage = "Platillo no encontrado."; }
            else if (!ent.Activo && !await _db.PlatPlatilloIngredientes.AnyAsync(x => x.PlatilloId == ent.Id))
            {
                // Publicar (activar) un platillo sin ingredientes lo haría pasar todos los filtros
                // del paciente por ausencia de datos. No se permite.
                ErrorMessage = $"No puedes publicar \"{ent.Nombre}\": no tiene ingredientes. El filtro no puede analizarlo; captúralos primero.";
            }
            else
            {
                ent.Activo = !ent.Activo;
                await _db.SaveChangesAsync();
                SuccessMessage = ent.Activo ? "Platillo reactivado." : "Platillo desactivado.";
            }
            // Preservar filtros al volver
            return RedirectToPage(new { q, categoriaId, pageNumber, pageSize = PageSize, mostrarInactivos });
        }
    }
}
