using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Models.Platillos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize(Roles = "Paciente,Administrador")]
    public class UsuarioAlimentacionModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UsuarioAlimentacionModel(ApplicationDbContext db) => _db = db;

        private static readonly string[] TiposValidos = { "Grupo", "Ingrediente", "Atributo" };

        // Catálogos disponibles para marcar
        public List<PlatGrupo> GruposDisponibles { get; set; } = new();
        public List<PlatAtributo> AtributosIntrinsecos { get; set; } = new();  // Ambito='Ingrediente'
        public List<PlatAtributo> AtributosUso { get; set; } = new();          // Ambito='Uso'

        // Exclusiones actuales del paciente
        public HashSet<int> ExcludedGrupoIds { get; set; } = new();
        public HashSet<int> ExcludedAtributoIds { get; set; } = new();
        public List<IngredienteExcluido> ExcludedIngredientes { get; set; } = new();

        public class IngredienteExcluido { public int Id { get; set; } public string Nombre { get; set; } = ""; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole("Medico")) return Redirect("/Identity/Medico/Dashboard");
            var userId = GetUserGuid();
            if (userId == null) return Page();

            GruposDisponibles = await _db.PlatGrupos.AsNoTracking()
                .Where(g => g.Activo).OrderBy(g => g.Orden).ThenBy(g => g.Nombre).ToListAsync();
            AtributosIntrinsecos = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Ingrediente").OrderBy(a => a.Nombre).ToListAsync();
            AtributosUso = await _db.PlatAtributos.AsNoTracking()
                .Where(a => a.Activo && a.Ambito == "Uso").OrderBy(a => a.Nombre).ToListAsync();

            var exclusiones = await _db.PlatPerfilExclusiones.AsNoTracking()
                .Where(e => e.idUsuario == userId.Value && !e.Eliminado)
                .Select(e => new { e.Tipo, e.RefId })
                .ToListAsync();

            ExcludedGrupoIds = exclusiones.Where(e => e.Tipo == "Grupo").Select(e => e.RefId).ToHashSet();
            ExcludedAtributoIds = exclusiones.Where(e => e.Tipo == "Atributo").Select(e => e.RefId).ToHashSet();

            var ingIds = exclusiones.Where(e => e.Tipo == "Ingrediente").Select(e => e.RefId).ToList();
            if (ingIds.Any())
            {
                ExcludedIngredientes = await _db.PlatIngredientes.AsNoTracking()
                    .Where(i => ingIds.Contains(i.Id))
                    .OrderBy(i => i.Nombre)
                    .Select(i => new IngredienteExcluido { Id = i.Id, Nombre = i.Nombre })
                    .ToListAsync();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAgregarExclusionAsync(string? tipo, int refId)
        {
            var userId = GetUserGuid();
            if (userId == null) return Unauthorized();
            tipo = (tipo ?? "").Trim();
            if (!TiposValidos.Contains(tipo))
                return new JsonResult(new { ok = false, mensaje = "Tipo inválido." }) { StatusCode = 400 };

            // El RefId debe existir y estar activo en su catálogo.
            var existe = tipo switch
            {
                "Grupo" => await _db.PlatGrupos.AnyAsync(g => g.Id == refId && g.Activo),
                "Ingrediente" => await _db.PlatIngredientes.AnyAsync(i => i.Id == refId && i.Activo),
                "Atributo" => await _db.PlatAtributos.AnyAsync(a => a.Id == refId && a.Activo),
                _ => false
            };
            if (!existe)
                return new JsonResult(new { ok = false, mensaje = "No encontrado." }) { StatusCode = 400 };

            // Si ya hay una fila (activa o borrada) para (usuario, tipo, refId), reactivamos en vez de duplicar.
            var fila = await _db.PlatPerfilExclusiones
                .FirstOrDefaultAsync(e => e.idUsuario == userId.Value && e.Tipo == tipo && e.RefId == refId);
            if (fila != null)
            {
                if (fila.Eliminado)
                {
                    fila.Eliminado = false;
                    fila.FechaEliminado = null;
                    fila.FechaCreacion = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
                return new JsonResult(new { ok = true, id = fila.Id });
            }

            var nueva = new PlatPerfilExclusion
            {
                idUsuario = userId.Value,
                Tipo = tipo,
                RefId = refId,
                FechaCreacion = DateTime.UtcNow,
                Eliminado = false
            };
            _db.PlatPerfilExclusiones.Add(nueva);
            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true, id = nueva.Id });
        }

        public async Task<IActionResult> OnPostEliminarExclusionAsync(string? tipo, int refId)
        {
            var userId = GetUserGuid();
            if (userId == null) return Unauthorized();
            tipo = (tipo ?? "").Trim();
            if (!TiposValidos.Contains(tipo))
                return new JsonResult(new { ok = false, mensaje = "Tipo inválido." }) { StatusCode = 400 };

            var fila = await _db.PlatPerfilExclusiones
                .FirstOrDefaultAsync(e => e.idUsuario == userId.Value && e.Tipo == tipo && e.RefId == refId && !e.Eliminado);
            if (fila != null)
            {
                fila.Eliminado = true;
                fila.FechaEliminado = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return new JsonResult(new { ok = true });
        }

        private Guid? GetUserGuid()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var g) ? g : (Guid?)null;
        }
    }
}
