using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Platillos
{
    // "¿Puedo comer queso?" — vista pública de ingrediente, ingrediente-primero.
    // Principio: evidencia, no veredicto. NUNCA dice sí/no. Ruta /Platillos/Ingrediente/{slug}.
    public class IngredienteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IngredienteModel(ApplicationDbContext db) => _db = db;

        public string Slug { get; private set; } = "";
        public int IngredienteId { get; private set; }
        public string Nombre { get; private set; } = "";
        public string GrupoNombre { get; private set; } = "";
        public string? GrupoNotasEII { get; private set; }
        public string? IngredienteNotasEII { get; private set; }
        public List<string> Atributos { get; private set; } = new();

        public bool ExcluidoPorTi { get; private set; }
        public string? MotivoExclusion { get; private set; }

        public List<string> PlatillosNombres { get; private set; } = new();
        public int PlatillosCount { get; private set; }

        public string MetaTitle { get; private set; } = "";
        public string MetaDescription { get; private set; } = "";

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();
            slug = slug.Trim().ToLowerInvariant();

            // Resolver el ingrediente por slug (catálogo chico y curado → match en memoria).
            var activos = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.Activo)
                .Select(i => new { i.Id, i.Nombre, i.GrupoId, i.NotasEII })
                .ToListAsync();
            var ing = activos.FirstOrDefault(i => SlugHelper.GenerateSlug(i.Nombre) == slug);
            if (ing == null) return NotFound();

            Slug = slug;
            IngredienteId = ing.Id;
            Nombre = ing.Nombre;
            IngredienteNotasEII = ing.NotasEII;

            var grupo = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Id == ing.GrupoId)
                .Select(g => new { g.Nombre, g.NotasEII }).FirstOrDefaultAsync();
            GrupoNombre = grupo?.Nombre ?? "";
            GrupoNotasEII = grupo?.NotasEII;

            // Atributos intrínsecos (cómo es siempre: gluten, lactosa, picante…).
            var atrIds = await _db.PlatIngredienteAtributos.AsNoTracking()
                .Where(a => a.IngredienteId == ing.Id)
                .Select(a => a.AtributoId).ToListAsync();
            if (atrIds.Any())
            {
                Atributos = await _db.PlatAtributos.AsNoTracking()
                    .Where(a => atrIds.Contains(a.Id))
                    .OrderBy(a => a.Nombre)
                    .Select(a => a.Nombre).ToListAsync();
            }

            // ¿El usuario autenticado lo tiene excluido? (directo, por grupo o por atributo).
            if ((User?.Identity?.IsAuthenticated ?? false)
                && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid))
            {
                var exclus = await _db.PlatPerfilExclusiones.AsNoTracking()
                    .Where(e => e.idUsuario == uid && !e.Eliminado)
                    .Select(e => new { e.Tipo, e.RefId }).ToListAsync();

                if (exclus.Any(e => e.Tipo == "Ingrediente" && e.RefId == ing.Id))
                {
                    ExcluidoPorTi = true;
                    MotivoExclusion = $"Marcaste que no toleras \"{Nombre}\".";
                }
                else if (exclus.Any(e => e.Tipo == "Grupo" && e.RefId == ing.GrupoId))
                {
                    ExcluidoPorTi = true;
                    MotivoExclusion = $"Tienes excluido el grupo \"{GrupoNombre}\", al que pertenece.";
                }
                else
                {
                    var atrExcl = exclus.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).ToHashSet();
                    if (atrExcl.Overlaps(atrIds))
                    {
                        ExcluidoPorTi = true;
                        MotivoExclusion = "Tienes excluida una característica de este alimento.";
                    }
                }
            }

            // Platillos ACTIVOS que lo contienen.
            var platIds = await _db.PlatPlatilloIngredientes.AsNoTracking()
                .Where(pi => pi.IngredienteId == ing.Id)
                .Select(pi => pi.PlatilloId).Distinct().ToListAsync();
            if (platIds.Any())
            {
                var plats = await _db.PlatPlatillos.AsNoTracking()
                    .Where(p => p.Activo && platIds.Contains(p.Id))
                    .OrderBy(p => p.Codigo)
                    .Select(p => p.Nombre).ToListAsync();
                PlatillosNombres = plats;
                PlatillosCount = plats.Count;
            }

            // Meta: en el idioma del paciente, no el del catálogo.
            MetaTitle = $"¿Puedo comer {Nombre} con colitis o Crohn (EII)?";
            MetaDescription = $"{Nombre} no está prohibido en la enfermedad inflamatoria intestinal; "
                + "lo que varía es la tolerancia de cada persona. Mira las notas, el contexto clínico "
                + "y los platillos que lo incluyen, y decídelo con tu médico.";

            return Page();
        }
    }
}
