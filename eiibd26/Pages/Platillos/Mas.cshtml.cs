using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eiibd26.Pages.Platillos
{
    // "Cargar más" del listado público. Espejo de Pages/Home/BlogMore. Público, sin [Authorize].
    // Usa el MISMO PlatilloFilter que Index: la página N respeta exactamente las mismas
    // exclusiones que la primera. Devuelve solo el HTML del partial + cabecera X-Total-Count.
    public class MasModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public MasModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true, Name = "q")] public string? SearchQuery { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 12;
        [BindProperty(SupportsGet = true, Name = "grupos")] public string? GruposCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "ingredientes")] public string? IngredientesCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "atributos")] public string? AtributosCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "categoria")] public int? Categoria { get; set; }
        [BindProperty(SupportsGet = true, Name = "verTodos")] public bool VerTodos { get; set; }
        [BindProperty(SupportsGet = true, Name = "f")] public bool Filtrado { get; set; }

        public List<PlatilloFilter.CardVm> Items { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 12;
            PageSize = Math.Min(PageSize, 50);

            Guid? userId = null;
            if ((User?.Identity?.IsAuthenticated ?? false)
                && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g)) userId = g;

            var res = await PlatilloFilter.EvaluarAsync(
                _db, userId, SearchQuery, Categoria,
                GruposCsv, IngredientesCsv, AtributosCsv,
                VerTodos, Filtrado, PageNumber, PageSize, needCercanos: false);

            Items = res.Cards;
            Response.Headers["X-Total-Count"] = res.TotalCumplen.ToString();
            return Page();
        }
    }
}
