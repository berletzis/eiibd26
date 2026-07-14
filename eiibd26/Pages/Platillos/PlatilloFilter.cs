using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Platillos
{
    /// <summary>
    /// Fuente ÚNICA de la lógica de filtrado del listado público de platillos.
    /// La comparten Index (primera página + chips) y Mas (Cargar más), para que la
    /// segunda página NUNCA filtre distinto a la primera: un platillo excluido por el
    /// perfil no puede colarse al paginar. El orden es estable (por Código) para que
    /// las páginas no se solapen ni salten registros.
    /// </summary>
    public static class PlatilloFilter
    {
        public class CardVm
        {
            public int Id; public string Codigo = ""; public string Nombre = "";
            public string? Categoria; public string? FuenteNombre; public string? FuenteUrl;
            public string? PasosResumidos; public int NumIngredientes;
        }

        public class CercanoVm { public string Nombre = ""; public List<string> Motivo = new(); }

        public class Resultado
        {
            public List<int> FilterGrupoIds = new();
            public List<int> FilterIngredienteIds = new();
            public List<int> FilterAtributoIds = new();
            public bool HasProfile;
            public bool UsingProfile;
            public bool HayExclusiones;
            public int TotalEnScope;    // Y (verificables en scope)
            public int TotalCumplen;    // X (pasan las exclusiones)
            public List<CardVm> Cards = new();         // solo la página pedida
            public List<CercanoVm> Cercanos = new();   // solo si needCercanos y no hay resultados
        }

        public static List<int> ParseIds(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            return csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => { int.TryParse(s.Trim(), out var v); return v; })
                .Where(v => v > 0).Distinct().ToList();
        }

        public static async Task<Resultado> EvaluarAsync(
            ApplicationDbContext db, Guid? userId,
            string? q, int? categoria,
            string? gruposCsv, string? ingredientesCsv, string? atributosCsv,
            bool verTodos, bool filtrado,
            int pageNumber, int pageSize, bool needCercanos)
        {
            var r = new Resultado();
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 12;

            // --- Exclusiones del perfil ---
            List<(string Tipo, int RefId)> perfil = new();
            if (userId != null)
            {
                perfil = (await db.PlatPerfilExclusiones.AsNoTracking()
                    .Where(e => e.idUsuario == userId.Value && !e.Eliminado)
                    .Select(e => new { e.Tipo, e.RefId }).ToListAsync())
                    .Select(e => (e.Tipo, e.RefId)).ToList();
            }
            r.HasProfile = perfil.Any();

            // ¿De dónde salen los filtros? Perfil en entrada fresca; query string si ya filtró.
            r.UsingProfile = userId != null && r.HasProfile && !verTodos && !filtrado;
            if (r.UsingProfile)
            {
                r.FilterGrupoIds = perfil.Where(e => e.Tipo == "Grupo").Select(e => e.RefId).Distinct().ToList();
                r.FilterIngredienteIds = perfil.Where(e => e.Tipo == "Ingrediente").Select(e => e.RefId).Distinct().ToList();
                r.FilterAtributoIds = perfil.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).Distinct().ToList();
            }
            else if (!verTodos)
            {
                r.FilterGrupoIds = ParseIds(gruposCsv);
                r.FilterIngredienteIds = ParseIds(ingredientesCsv);
                r.FilterAtributoIds = ParseIds(atributosCsv);
            }
            // verTodos => sin filtros de exclusión.

            var exGrupos = r.FilterGrupoIds.ToHashSet();
            var exIngredientes = r.FilterIngredienteIds.ToHashSet();
            var exAtributos = r.FilterAtributoIds.ToHashSet();

            // --- Platillos en scope: activos + búsqueda + categoría (en DB), ordenados por Código ---
            var baseQuery = db.PlatPlatillos.AsNoTracking().Where(p => p.Activo);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                baseQuery = baseQuery.Where(p => p.Nombre.Contains(term) || p.Codigo.Contains(term));
            }
            if (categoria.HasValue)
                baseQuery = baseQuery.Where(p => p.CategoriaId == categoria.Value);

            var platillos = await baseQuery
                .OrderBy(p => p.Codigo)   // orden estable para paginar sin solapes
                .Select(p => new
                {
                    p.Id, p.Codigo, p.Nombre,
                    Categoria = p.Categoria != null ? p.Categoria.Nombre : null,
                    p.FuenteNombre, p.FuenteUrl, p.PasosResumidos
                })
                .ToListAsync();

            var platIds = platillos.Select(p => p.Id).ToList();

            var rows = await db.PlatPlatilloIngredientes.AsNoTracking()
                .Where(x => platIds.Contains(x.PlatilloId))
                .Select(x => new { x.Id, x.PlatilloId, x.IngredienteId })
                .ToListAsync();

            // SEGURIDAD: un platillo sin ingredientes no es verificable → fuera del listado público.
            var conIngredientes = rows.Select(x => x.PlatilloId).ToHashSet();
            platillos = platillos.Where(p => conIngredientes.Contains(p.Id)).ToList();
            r.TotalEnScope = platillos.Count;

            var rowIds = rows.Select(x => x.Id).ToList();
            var ingIds = rows.Select(x => x.IngredienteId).Distinct().ToList();

            var usoPorRow = (await db.PlatPlatilloIngredienteAtributos.AsNoTracking()
                .Where(a => rowIds.Contains(a.PlatilloIngredienteId))
                .Select(a => new { a.PlatilloIngredienteId, a.AtributoId }).ToListAsync())
                .GroupBy(a => a.PlatilloIngredienteId)
                .ToDictionary(gp => gp.Key, gp => gp.Select(x => x.AtributoId).ToHashSet());

            var ingGrupo = await db.PlatIngredientes.AsNoTracking()
                .Where(i => ingIds.Contains(i.Id))
                .Select(i => new { i.Id, i.GrupoId, i.Nombre }).ToListAsync();
            var grupoDeIng = ingGrupo.ToDictionary(i => i.Id, i => i.GrupoId);
            var nombreDeIng = ingGrupo.ToDictionary(i => i.Id, i => i.Nombre);

            var intrinsecoPorIng = (await db.PlatIngredienteAtributos.AsNoTracking()
                .Where(a => ingIds.Contains(a.IngredienteId))
                .Select(a => new { a.IngredienteId, a.AtributoId }).ToListAsync())
                .GroupBy(a => a.IngredienteId)
                .ToDictionary(gp => gp.Key, gp => gp.Select(x => x.AtributoId).ToHashSet());

            var rowsPorPlatillo = rows.GroupBy(x => x.PlatilloId)
                .ToDictionary(gp => gp.Key, gp => gp.ToList());

            // --- Evaluar la regla de descarte (platillos ya vienen en orden de Código) ---
            r.HayExclusiones = exGrupos.Any() || exIngredientes.Any() || exAtributos.Any();
            var cumplen = new List<int>();
            var incumplen = new List<(int Id, string Nombre, List<string> Motivo)>();

            foreach (var p in platillos)
            {
                var motivos = new List<string>();
                if (r.HayExclusiones && rowsPorPlatillo.TryGetValue(p.Id, out var pr))
                {
                    var offending = new HashSet<string>();
                    foreach (var row in pr)
                    {
                        bool bad = false;
                        if (grupoDeIng.TryGetValue(row.IngredienteId, out var gid) && exGrupos.Contains(gid)) bad = true;
                        if (!bad && exIngredientes.Contains(row.IngredienteId)) bad = true;
                        if (!bad && intrinsecoPorIng.TryGetValue(row.IngredienteId, out var intr) && intr.Overlaps(exAtributos)) bad = true;
                        if (!bad && usoPorRow.TryGetValue(row.Id, out var uso) && uso.Overlaps(exAtributos)) bad = true;
                        if (bad && nombreDeIng.TryGetValue(row.IngredienteId, out var nom)) offending.Add(nom);
                    }
                    motivos = offending.OrderBy(x => x).ToList();
                }

                if (motivos.Count == 0) cumplen.Add(p.Id);
                else incumplen.Add((p.Id, p.Nombre, motivos));
            }

            r.TotalCumplen = cumplen.Count;

            if (r.TotalCumplen > 0)
            {
                var byId = platillos.ToDictionary(p => p.Id);
                var pageIds = cumplen.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                foreach (var id in pageIds)
                {
                    var p = byId[id];
                    r.Cards.Add(new CardVm
                    {
                        Id = p.Id, Codigo = p.Codigo, Nombre = p.Nombre, Categoria = p.Categoria,
                        FuenteNombre = p.FuenteNombre, FuenteUrl = p.FuenteUrl, PasosResumidos = p.PasosResumidos,
                        NumIngredientes = rowsPorPlatillo.TryGetValue(p.Id, out var rr) ? rr.Count : 0
                    });
                }
            }
            else if (needCercanos && r.HayExclusiones && r.TotalEnScope > 0)
            {
                r.Cercanos = incumplen
                    .OrderBy(x => x.Motivo.Count).ThenBy(x => x.Nombre)
                    .Take(6)
                    .Select(x => new CercanoVm { Nombre = x.Nombre, Motivo = x.Motivo })
                    .ToList();
            }

            return r;
        }
    }
}
