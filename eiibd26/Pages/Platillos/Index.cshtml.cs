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
        [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 9;
        [BindProperty(SupportsGet = true, Name = "grupos")] public string? GruposCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "ingredientes")] public string? IngredientesCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "atributos")] public string? AtributosCsv { get; set; }
        [BindProperty(SupportsGet = true, Name = "categoria")] public int? Categoria { get; set; }
        [BindProperty(SupportsGet = true, Name = "verTodos")] public bool VerTodos { get; set; }
        // f=1 => el query string manda (el usuario está filtrando). Sin f => perfil (entrada fresca).
        [BindProperty(SupportsGet = true, Name = "f")] public bool Filtrado { get; set; }

        // Filtros efectivos aplicados
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
        public int TotalPages { get; set; }
        public List<CardVm> Items { get; set; } = new();
        public List<CercanoVm> Cercanos { get; set; } = new();
        public List<CatVm> Categorias { get; set; } = new();

        public class Chip { public string Tipo = ""; public int RefId; public string Nombre = ""; }
        public class CatVm { public int Id; public string Nombre = ""; }
        public class CardVm
        {
            public int Id; public string Codigo = ""; public string Nombre = "";
            public string? Categoria; public string? FuenteNombre; public string? FuenteUrl;
            public string? PasosResumidos; public int NumIngredientes;
        }
        public class CercanoVm { public string Nombre = ""; public List<string> Motivo = new(); }

        private static List<int> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => { int.TryParse(s.Trim(), out var v); return v; })
                .Where(v => v > 0).Distinct().ToList();
        }

        public async Task OnGetAsync()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 9;
            PageSize = Math.Min(PageSize, 50);

            IsAuth = User?.Identity?.IsAuthenticated ?? false;
            Guid? userId = null;
            if (IsAuth && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g)) userId = g;

            // Exclusiones del perfil (para saber si tiene perfil y para el modo "perfil").
            List<(string Tipo, int RefId)> perfil = new();
            if (userId != null)
            {
                perfil = (await _db.PlatPerfilExclusiones.AsNoTracking()
                    .Where(e => e.idUsuario == userId.Value && !e.Eliminado)
                    .Select(e => new { e.Tipo, e.RefId }).ToListAsync())
                    .Select(e => (e.Tipo, e.RefId)).ToList();
            }
            HasProfile = perfil.Any();

            // ¿De dónde salen los filtros? Perfil en entrada fresca; query string si el usuario ya filtró.
            UsingProfile = IsAuth && HasProfile && !VerTodos && !Filtrado;
            if (UsingProfile)
            {
                FilterGrupoIds = perfil.Where(e => e.Tipo == "Grupo").Select(e => e.RefId).Distinct().ToList();
                FilterIngredienteIds = perfil.Where(e => e.Tipo == "Ingrediente").Select(e => e.RefId).Distinct().ToList();
                FilterAtributoIds = perfil.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).Distinct().ToList();
            }
            else if (!VerTodos)
            {
                FilterGrupoIds = ParseIds(GruposCsv);
                FilterIngredienteIds = ParseIds(IngredientesCsv);
                FilterAtributoIds = ParseIds(AtributosCsv);
            }
            // VerTodos => las tres listas quedan vacías (sin filtro de exclusión).

            var exGrupos = FilterGrupoIds.ToHashSet();
            var exIngredientes = FilterIngredienteIds.ToHashSet();
            var exAtributos = FilterAtributoIds.ToHashSet();

            // Combo de categorías (activas).
            Categorias = (await _db.PlatCategorias.AsNoTracking()
                .Where(c => c.Activo).OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                .Select(c => new { c.Id, c.Nombre }).ToListAsync())
                .Select(c => new CatVm { Id = c.Id, Nombre = c.Nombre }).ToList();

            // --- Platillos en scope: activos + búsqueda + categoría (en DB) ---
            var baseQuery = _db.PlatPlatillos.AsNoTracking().Where(p => p.Activo);
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var term = SearchQuery.Trim();
                baseQuery = baseQuery.Where(p => p.Nombre.Contains(term) || p.Codigo.Contains(term));
            }
            if (Categoria.HasValue)
                baseQuery = baseQuery.Where(p => p.CategoriaId == Categoria.Value);

            var platillos = await baseQuery
                .Select(p => new
                {
                    p.Id, p.Codigo, p.Nombre, p.CategoriaId,
                    Categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                    p.FuenteNombre, p.FuenteUrl, p.PasosResumidos
                })
                .ToListAsync();

            var platIds = platillos.Select(p => p.Id).ToList();

            // --- Cargar datos de ingredientes de esos platillos (para la regla de descarte) ---
            var rows = await _db.PlatPlatilloIngredientes.AsNoTracking()
                .Where(r => platIds.Contains(r.PlatilloId))
                .Select(r => new { r.Id, r.PlatilloId, r.IngredienteId })
                .ToListAsync();

            // SEGURIDAD: un platillo sin ingredientes NO es verificable. No sabemos qué contiene,
            // así que no puede presentarse como "cumple tu perfil": se excluye del listado público.
            // (Además el admin ya no puede publicar platillos vacíos; esto cubre datos heredados.)
            var conIngredientes = rows.Select(r => r.PlatilloId).ToHashSet();
            platillos = platillos.Where(p => conIngredientes.Contains(p.Id)).ToList();
            TotalEnScope = platillos.Count;
            var rowIds = rows.Select(r => r.Id).ToList();
            var ingIds = rows.Select(r => r.IngredienteId).Distinct().ToList();

            var usoPorRow = (await _db.PlatPlatilloIngredienteAtributos.AsNoTracking()
                .Where(a => rowIds.Contains(a.PlatilloIngredienteId))
                .Select(a => new { a.PlatilloIngredienteId, a.AtributoId }).ToListAsync())
                .GroupBy(a => a.PlatilloIngredienteId)
                .ToDictionary(gp => gp.Key, gp => gp.Select(x => x.AtributoId).ToHashSet());

            var ingGrupo = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => ingIds.Contains(i.Id))
                .Select(i => new { i.Id, i.GrupoId, i.Nombre }).ToListAsync();
            var grupoDeIng = ingGrupo.ToDictionary(i => i.Id, i => i.GrupoId);
            var nombreDeIng = ingGrupo.ToDictionary(i => i.Id, i => i.Nombre);

            var intrinsecoPorIng = (await _db.PlatIngredienteAtributos.AsNoTracking()
                .Where(a => ingIds.Contains(a.IngredienteId))
                .Select(a => new { a.IngredienteId, a.AtributoId }).ToListAsync())
                .GroupBy(a => a.IngredienteId)
                .ToDictionary(gp => gp.Key, gp => gp.Select(x => x.AtributoId).ToHashSet());

            var rowsPorPlatillo = rows.GroupBy(r => r.PlatilloId)
                .ToDictionary(gp => gp.Key, gp => gp.ToList());

            // --- Evaluar cada platillo: ¿qué ingredientes incumplen? ---
            bool hayExclusiones = exGrupos.Any() || exIngredientes.Any() || exAtributos.Any();
            var cumplen = new List<int>();
            var incumplen = new List<(int Id, string Nombre, List<string> Motivo)>();

            foreach (var p in platillos)
            {
                var motivos = new List<string>();
                if (hayExclusiones && rowsPorPlatillo.TryGetValue(p.Id, out var pr))
                {
                    var offending = new HashSet<string>();
                    foreach (var r in pr)
                    {
                        bool bad = false;
                        if (grupoDeIng.TryGetValue(r.IngredienteId, out var gid) && exGrupos.Contains(gid)) bad = true;
                        if (!bad && exIngredientes.Contains(r.IngredienteId)) bad = true;
                        if (!bad && intrinsecoPorIng.TryGetValue(r.IngredienteId, out var intr) && intr.Overlaps(exAtributos)) bad = true;
                        if (!bad && usoPorRow.TryGetValue(r.Id, out var uso) && uso.Overlaps(exAtributos)) bad = true;
                        if (bad && nombreDeIng.TryGetValue(r.IngredienteId, out var nom)) offending.Add(nom);
                    }
                    motivos = offending.OrderBy(x => x).ToList();
                }

                if (motivos.Count == 0) cumplen.Add(p.Id);
                else incumplen.Add((p.Id, p.Nombre, motivos));
            }

            TotalCumplen = cumplen.Count;
            TotalPages = TotalCumplen == 0 ? 1 : (int)Math.Ceiling(TotalCumplen / (double)PageSize);
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            if (TotalCumplen > 0)
            {
                var pageIds = cumplen.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToHashSet();
                Items = platillos.Where(p => pageIds.Contains(p.Id))
                    .OrderBy(p => p.Codigo)
                    .Select(p => new CardVm
                    {
                        Id = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, Categoria = p.Categoria,
                        FuenteNombre = p.FuenteNombre, FuenteUrl = p.FuenteUrl, PasosResumidos = p.PasosResumidos,
                        NumIngredientes = rowsPorPlatillo.TryGetValue(p.Id, out var rr) ? rr.Count : 0
                    }).ToList();
            }
            else if (hayExclusiones && TotalEnScope > 0)
            {
                // Estado vacío: los más cercanos (menos incumplimientos), con su motivo.
                Cercanos = incumplen
                    .OrderBy(x => x.Motivo.Count).ThenBy(x => x.Nombre)
                    .Take(6)
                    .Select(x => new CercanoVm { Nombre = x.Nombre, Motivo = x.Motivo })
                    .ToList();
            }

            // --- Chips de filtros activos (nombres) ---
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
        // Siempre agrega f=1: al quitar un chip pasamos a modo query-string, sin tocar la base.
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
            var qs = new List<string> { "f=1" };
            if (!string.IsNullOrWhiteSpace(SearchQuery)) qs.Add("q=" + Uri.EscapeDataString(SearchQuery));
            if (Categoria.HasValue) qs.Add("categoria=" + Categoria.Value);
            if (g.Any()) qs.Add("grupos=" + string.Join(",", g));
            if (i.Any()) qs.Add("ingredientes=" + string.Join(",", i));
            if (a.Any()) qs.Add("atributos=" + string.Join(",", a));
            return "/Platillos?" + string.Join("&", qs);
        }

        // Paginación: conserva búsqueda, categoría, filtros activos y el modo (verTodos / query-string).
        public string BuildPageUrl(int page)
        {
            var qs = new List<string>();
            qs.Add(VerTodos ? "verTodos=1" : "f=1");
            if (!string.IsNullOrWhiteSpace(SearchQuery)) qs.Add("q=" + Uri.EscapeDataString(SearchQuery));
            if (Categoria.HasValue) qs.Add("categoria=" + Categoria.Value);
            if (!VerTodos)
            {
                if (FilterGrupoIds.Any()) qs.Add("grupos=" + string.Join(",", FilterGrupoIds));
                if (FilterIngredienteIds.Any()) qs.Add("ingredientes=" + string.Join(",", FilterIngredienteIds));
                if (FilterAtributoIds.Any()) qs.Add("atributos=" + string.Join(",", FilterAtributoIds));
            }
            if (page > 1) qs.Add("PageNumber=" + page);
            return "/Platillos?" + string.Join("&", qs);
        }

        // "Ver todos los platillos" — sin filtros de exclusión, conservando búsqueda/categoría.
        public string BuildVerTodosUrl()
        {
            var qs = new List<string> { "verTodos=1" };
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
