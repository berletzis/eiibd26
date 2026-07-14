using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Platillos
{
    // Pública, sin [Authorize] — espejo de Pages/Contenidos/Index.
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true, Name = "q")] public string? SearchQuery { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 12;
        [BindProperty(SupportsGet = true, Name = "grupos")] public string? GruposCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "ingredientes")] public string? IngredientesCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "atributos")] public string? AtributosCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "categoria")] public int? Categoria { get; set; }
        [BindProperty(SupportsGet = true, Name = "verTodos")] public bool VerTodos { get; set; }
        // f=true => el query string manda (el usuario está filtrando). Sin f => perfil (entrada fresca).
        [BindProperty(SupportsGet = true, Name = "f")] public bool Filtrado { get; set; }

        // Filtros efectivos aplicados (los resuelve PlatilloFilter; los usamos para chips + Build*Url)
        public List<int> FilterGrupoIds { get; set; } = new();
        public List<int> FilterIngredienteIds { get; set; } = new();
        public List<int> FilterAtributoIds { get; set; } = new();

        // Estado para la vista
        public bool IsAuth { get; set; }
        public bool HasProfile { get; set; }
        public bool UsingProfile { get; set; }
        public List<Chip> Chips { get; set; } = new();
        public int TotalEnScope { get; set; }   // Y
        public int TotalCumplen { get; set; }    // X
        public List<PlatilloFilter.CardVm> Items { get; set; } = new();
        public List<PlatilloFilter.CercanoVm> Cercanos { get; set; } = new();
        public List<CatVm> Categorias { get; set; } = new();

        public class Chip { public string Tipo = ""; public int RefId; public string Nombre = ""; }
        public class CatVm { public int Id; public string Nombre = ""; }

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 12;
            PageSize = Math.Min(PageSize, 50);

            IsAuth = User?.Identity?.IsAuthenticated ?? false;
            Guid? userId = null;
            if (IsAuth && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g)) userId = g;

            // Toda la lógica de filtrado/descarte vive en PlatilloFilter (misma fuente que /Platillos/Mas).
            var res = await PlatilloFilter.EvaluarAsync(
                _db, userId, SearchQuery, Categoria,
                GruposCsv, IngredientesCsv, AtributosCsv,
                VerTodos, Filtrado, PageNumber, PageSize, needCercanos: true);

            FilterGrupoIds = res.FilterGrupoIds;
            FilterIngredienteIds = res.FilterIngredienteIds;
            FilterAtributoIds = res.FilterAtributoIds;
            HasProfile = res.HasProfile;
            UsingProfile = res.UsingProfile;
            TotalEnScope = res.TotalEnScope;
            TotalCumplen = res.TotalCumplen;
            Items = res.Cards;
            Cercanos = res.Cercanos;

            // Combo de categorías (activas) — solo lo necesita la vista completa.
            Categorias = (await _db.PlatCategorias.AsNoTracking()
                .Where(c => c.Activo).OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                .Select(c => new { c.Id, c.Nombre }).ToListAsync())
                .Select(c => new CatVm { Id = c.Id, Nombre = c.Nombre }).ToList();

            // --- Chips de filtros activos (nombres) ---
            var exGrupos = FilterGrupoIds.ToHashSet();
            var exIngredientes = FilterIngredienteIds.ToHashSet();
            var exAtributos = FilterAtributoIds.ToHashSet();
            if (exGrupos.Any())
            {
                var chGrupos = await _db.PlatGrupos.AsNoTracking().Where(x => exGrupos.Contains(x.Id))
                    .Select(x => new { x.Id, x.Nombre }).ToListAsync();
                Chips.AddRange(chGrupos.Select(x => new Chip { Tipo = "grupos", RefId = x.Id, Nombre = x.Nombre }));
            }
            if (exAtributos.Any())
            {
                var chAtrib = await _db.PlatAtributos.AsNoTracking().Where(x => exAtributos.Contains(x.Id))
                    .Select(x => new { x.Id, x.Nombre }).ToListAsync();
                Chips.AddRange(chAtrib.Select(x => new Chip { Tipo = "atributos", RefId = x.Id, Nombre = x.Nombre }));
            }
            if (exIngredientes.Any())
            {
                var chIng = await _db.PlatIngredientes.AsNoTracking().Where(x => exIngredientes.Contains(x.Id))
                    .Select(x => new { x.Id, x.Nombre }).ToListAsync();
                Chips.AddRange(chIng.Select(x => new Chip { Tipo = "ingredientes", RefId = x.Id, Nombre = x.Nombre }));
            }
        }

        // Construye el query string quitando un filtro (para el link de "quitar chip").
        // Siempre agrega f=true: al quitar un chip pasamos a modo query-string, sin tocar la base.
        public string BuildRemoveUrl(string tipo, int refId)
        {
            var g = new List<int>(FilterGrupoIds);
            var i = new List<int>(FilterIngredienteIds);
            var a = new List<int>(FilterAtributoIds);
            if (tipo == "grupos") g.Remove(refId);
            else if (tipo == "ingredientes") i.Remove(refId);
            else if (tipo == "atributos") a.Remove(refId);
            return BuildUrl(g, i, a);
        }

        public string BuildUrl(List<int> g, List<int> i, List<int> a)
        {
            // Ojo: el binder de bool acepta "true"/"false", NO "1". Emitir "true" o queda en false.
            var qs = new List<string> { "f=true" };
            if (!string.IsNullOrWhiteSpace(SearchQuery)) qs.Add("q=" + Uri.EscapeDataString(SearchQuery));
            if (Categoria.HasValue) qs.Add("categoria=" + Categoria.Value);
            if (g.Any()) qs.Add("grupos=" + string.Join(",", g));
            if (i.Any()) qs.Add("ingredientes=" + string.Join(",", i));
            if (a.Any()) qs.Add("atributos=" + string.Join(",", a));
            return "/Platillos?" + string.Join("&", qs);
        }

        // "Ver todos los platillos" — sin filtros de exclusión, conservando búsqueda/categoría.
        public string BuildVerTodosUrl()
        {
            var qs = new List<string> { "verTodos=true" };
            if (!string.IsNullOrWhiteSpace(SearchQuery)) qs.Add("q=" + Uri.EscapeDataString(SearchQuery));
            if (Categoria.HasValue) qs.Add("categoria=" + Categoria.Value);
            return "/Platillos?" + string.Join("&", qs);
        }

        // "Volver a mi perfil" — sin f ni verTodos, así se recalcula UsingProfile.
        public string BuildPerfilUrl()
        {
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(SearchQuery)) qs.Add("q=" + Uri.EscapeDataString(SearchQuery));
            if (Categoria.HasValue) qs.Add("categoria=" + Categoria.Value);
            return qs.Any() ? "/Platillos?" + string.Join("&", qs) : "/Platillos";
        }
    }
}
