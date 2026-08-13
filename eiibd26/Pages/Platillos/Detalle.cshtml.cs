using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Services.Platillos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Platillos
{
    // Detalle público de un platillo. Ruta /Platillos/{slug} (AddPageRoute). Server-rendered, indexable.
    // SEGURIDAD: solo platillos Activo con >=1 ingrediente (misma regla que el listado): un platillo
    // sin renglones no es verificable → no se expone.
    public class DetalleModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IPlatNotaClinicaService _notas;
        public DetalleModel(ApplicationDbContext db, IPlatNotaClinicaService notas)
        {
            _db = db;
            _notas = notas;
        }

        public string Slug { get; private set; } = "";
        public int PlatilloId { get; private set; }
        public string Nombre { get; private set; } = "";
        public string? Categoria { get; private set; }
        public int? Porciones { get; private set; }
        public int? TiempoPrepMin { get; private set; }
        public string? Dificultad { get; private set; }
        public string? PasosResumidos { get; private set; }
        public string? FuenteNombre { get; private set; }
        public string? FuenteUrl { get; private set; }

        public List<RowVm> Ingredientes { get; private set; } = new();
        public class RowVm
        {
            public string Nombre = "";
            public string Slug = "";
            // Grupo del ingrediente: da el color de su moneda y el fallback del ícono (ingrediente → grupo).
            public string Grupo = "";
            public decimal? Cantidad;
            public string? Unidad;
            public bool EsAlGusto;
            public string? NotaPreparacion;
            public List<string> Usos = new();   // crudo / frito / en jugo
            // ¿Hay contenido clínico PUBLICADO que ver en su página? (su nota o la de su grupo).
            // Se determina por el candado (bulk); si una nota se despublica, esto se apaga solo.
            public bool TieneNota;
        }

        // ¿Cómo encaja contigo?
        public bool IsAuth { get; private set; }
        public bool HasProfile { get; private set; }
        public bool Cumple { get; private set; }
        public List<EncajeItem> Rompen { get; private set; } = new();
        public class EncajeItem { public string Ingrediente = ""; public string Detalle = ""; }
        // Nombres de TODO lo que el usuario declaró que no tolera (grupos + características + ingredientes),
        // para enlistar en el callout de encaje las intolerancias que este platillo respeta.
        public List<string> Exclusiones { get; private set; } = new();

        public string MetaTitle { get; private set; } = "";
        public string MetaDescription { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            slug = slug.Trim().ToLowerInvariant();

            // Resolver por slug (catálogo chico). Orden por Código → empate determinista si dos comparten slug.
            var activos = await _db.PlatPlatillos.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Codigo)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();
            var match = activos.FirstOrDefault(p => SlugHelper.GenerateSlug(p.Nombre) == slug);
            if (match == null) return NotFound();

            var p = await _db.PlatPlatillos.AsNoTracking()
                .Include(x => x.Categoria)
                .Include(x => x.Ingredientes).ThenInclude(r => r.Atributos)
                .FirstOrDefaultAsync(x => x.Id == match.Id && x.Activo);
            if (p == null) return NotFound();

            // SEGURIDAD: sin ingredientes no es verificable → no se muestra (igual que el listado público).
            if (p.Ingredientes == null || p.Ingredientes.Count == 0) return NotFound();

            Slug = slug;
            PlatilloId = p.Id;
            Nombre = p.Nombre;
            Categoria = p.Categoria?.Nombre;
            Porciones = p.Porciones;
            TiempoPrepMin = p.TiempoPrepMin;
            Dificultad = p.Dificultad;
            PasosResumidos = p.PasosResumidos;
            FuenteNombre = p.FuenteNombre;
            FuenteUrl = p.FuenteUrl;

            var rows = p.Ingredientes.OrderBy(r => r.Id).ToList();
            var ingIds = rows.Select(r => r.IngredienteId).Distinct().ToList();
            var unidadIds = rows.Where(r => r.UnidadId.HasValue).Select(r => r.UnidadId!.Value).Distinct().ToList();
            var usoIds = rows.SelectMany(r => r.Atributos.Select(a => a.AtributoId)).Distinct().ToList();

            var ingInfo = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => ingIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Nombre, i.GrupoId }).ToListAsync();
            var ingNombre = ingInfo.ToDictionary(i => i.Id, i => i.Nombre);
            var ingGrupo = ingInfo.ToDictionary(i => i.Id, i => i.GrupoId);

            // Nombre del grupo de cada ingrediente — para el color de la moneda y el fallback del ícono.
            var grupoIds = ingInfo.Select(i => i.GrupoId).Distinct().ToList();
            var grupoNombrePorId = await _db.PlatGrupos.AsNoTracking()
                .Where(g => grupoIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Nombre);

            var unidades = await _db.PlatUnidades.AsNoTracking()
                .Where(u => unidadIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);

            var atrNombre = await _db.PlatAtributos.AsNoTracking()
                .Where(a => usoIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Nombre);

            // Badge "¿Puedo comerlo?": ¿el ingrediente tiene contenido clínico PUBLICADO que ver — su
            // propia nota o la de su grupo (ambas se muestran en su página)? Vía el candado (bulk, sin
            // N+1); NUNCA una query propia que pueda colar borradores. Si una nota se despublica, el
            // badge se apaga solo porque depende del mismo servicio.
            var ingConNota = await _notas.ObtenerDestinosConNotaVisibleAsync("Ingrediente");
            var grupoConNota = await _notas.ObtenerDestinosConNotaVisibleAsync("Grupo");

            foreach (var r in rows)
            {
                var nom = ingNombre.TryGetValue(r.IngredienteId, out var nn) ? nn : "(ingrediente)";
                var tieneNota = ingConNota.Contains(r.IngredienteId)
                    || (ingGrupo.TryGetValue(r.IngredienteId, out var gid) && grupoConNota.Contains(gid));
                Ingredientes.Add(new RowVm
                {
                    Nombre = nom,
                    Slug = SlugHelper.GenerateSlug(nom),
                    Grupo = ingGrupo.TryGetValue(r.IngredienteId, out var gidRow)
                            && grupoNombrePorId.TryGetValue(gidRow, out var gnRow) ? gnRow : "",
                    Cantidad = r.EsAlGusto ? null : r.Cantidad,
                    Unidad = r.UnidadId.HasValue && unidades.TryGetValue(r.UnidadId.Value, out var un) ? un : null,
                    EsAlGusto = r.EsAlGusto,
                    NotaPreparacion = r.NotaPreparacion,
                    Usos = r.Atributos
                        .Select(a => atrNombre.TryGetValue(a.AtributoId, out var an) ? an : null)
                        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).OrderBy(x => x).ToList(),
                    TieneNota = tieneNota
                });
            }

            // ¿Cómo encaja contigo? — reusa la misma regla de exclusión que PlatilloFilter, con motivo por ingrediente.
            IsAuth = User?.Identity?.IsAuthenticated ?? false;
            Guid? uid = null;
            if (IsAuth && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var g)) uid = g;
            if (uid != null)
            {
                var perfil = await _db.PlatPerfilExclusiones.AsNoTracking()
                    .Where(e => e.idUsuario == uid.Value && !e.Eliminado)
                    .Select(e => new { e.Tipo, e.RefId }).ToListAsync();
                HasProfile = perfil.Any();
                if (HasProfile)
                {
                    var exGrupos = perfil.Where(e => e.Tipo == "Grupo").Select(e => e.RefId).ToHashSet();
                    var exIng = perfil.Where(e => e.Tipo == "Ingrediente").Select(e => e.RefId).ToHashSet();
                    var exAtr = perfil.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).ToHashSet();

                    var grupoNombre = await _db.PlatGrupos.AsNoTracking()
                        .Where(x => exGrupos.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id, x => x.Nombre);
                    var intrinsecoPorIng = (await _db.PlatIngredienteAtributos.AsNoTracking()
                        .Where(a => ingIds.Contains(a.IngredienteId))
                        .Select(a => new { a.IngredienteId, a.AtributoId }).ToListAsync())
                        .GroupBy(a => a.IngredienteId).ToDictionary(gp => gp.Key, gp => gp.Select(x => x.AtributoId).ToHashSet());
                    var atrExclNombre = await _db.PlatAtributos.AsNoTracking()
                        .Where(a => exAtr.Contains(a.Id))
                        .ToDictionaryAsync(a => a.Id, a => a.Nombre);
                    var exIngNombre = await _db.PlatIngredientes.AsNoTracking()
                        .Where(i => exIng.Contains(i.Id))
                        .Select(i => i.Nombre).ToListAsync();

                    // Lista honesta para el callout: lo que el usuario declaró que no tolera, agrupado por
                    // tipo (grupos → características → ingredientes) y ordenado dentro de cada uno.
                    Exclusiones = grupoNombre.Values.OrderBy(n => n)
                        .Concat(atrExclNombre.Values.OrderBy(n => n))
                        .Concat(exIngNombre.OrderBy(n => n))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList();

                    var vistos = new HashSet<int>();
                    foreach (var r in rows)
                    {
                        if (!vistos.Add(r.IngredienteId)) continue;   // un ingrediente rompe una sola vez
                        var ingId = r.IngredienteId;
                        var nom = ingNombre.TryGetValue(ingId, out var nn2) ? nn2 : "(ingrediente)";
                        string? detalle = null;

                        if (exIng.Contains(ingId))
                            detalle = "que está en tu lista";
                        else if (ingGrupo.TryGetValue(ingId, out var gid) && exGrupos.Contains(gid))
                            detalle = $"del grupo {(grupoNombre.TryGetValue(gid, out var gn) ? gn : "excluido")}, que también excluiste";
                        else if (intrinsecoPorIng.TryGetValue(ingId, out var intr) && intr.Overlaps(exAtr))
                        {
                            var atrId = intr.First(a => exAtr.Contains(a));
                            detalle = $"por su contenido de {(atrExclNombre.TryGetValue(atrId, out var an2) ? an2 : "algo")}, que excluiste";
                        }
                        else
                        {
                            var rowUso = r.Atributos.Select(a => a.AtributoId).ToHashSet();
                            if (rowUso.Overlaps(exAtr))
                            {
                                var atrId = rowUso.First(a => exAtr.Contains(a));
                                detalle = $"porque va {(atrExclNombre.TryGetValue(atrId, out var an3) ? an3 : "así")}, que excluiste";
                            }
                        }

                        if (detalle != null)
                            Rompen.Add(new EncajeItem { Ingrediente = nom, Detalle = detalle });
                    }
                    Cumple = Rompen.Count == 0;
                }
            }

            MetaTitle = $"{Nombre}: ingredientes y si puedes comerlo con EII";
            MetaDescription = $"Ingredientes, preparación y contexto para la enfermedad inflamatoria intestinal (Crohn y colitis) de {Nombre}. "
                + "Mira qué lleva, cuáles van crudos o fritos, y decide con tu médico si encaja contigo.";
            return Page();
        }
    }
}
