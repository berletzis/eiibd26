using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using eiibd26.Data;
using eiibd26.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eiibd26.Controllers
{
    /// <summary>
    /// Generación de contenido del módulo Platillos con IA (hermano de SintomasAdminController).
    /// REGLA: estos endpoints SOLO generan y devuelven texto. NUNCA guardan ni publican — el humano
    /// revisa en el editor y guarda desde el CRUD. Ninguna nota nace publicada por esta vía.
    /// </summary>
    [Authorize(Roles = "Administrador")]
    [ApiController]
    [Route("api/admin/platillos")]
    public class PlatillosAiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPlatillosAiService _ai;
        private readonly ILogger<PlatillosAiController> _logger;

        public PlatillosAiController(
            ApplicationDbContext db,
            IPlatillosAiService ai,
            ILogger<PlatillosAiController> logger)
        {
            _db = db;
            _ai = ai;
            _logger = logger;
        }

        // ─── Nota clínica de GRUPO ────────────────────────────────────────────────────
        [HttpPost("grupo/{id:int}/generate-nota")]
        public async Task<IActionResult> GenerateNotaGrupo(int id, CancellationToken ct)
        {
            var grupo = await _db.PlatGrupos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
            if (grupo == null) return NotFound(new { ok = false, error = "Grupo no encontrado" });
            if (string.IsNullOrWhiteSpace(grupo.Nombre))
                return BadRequest(new { ok = false, error = "El grupo no tiene nombre" });

            var ingredientes = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.GrupoId == id)
                .OrderBy(i => i.Nombre)
                .Select(i => i.Nombre)
                .ToListAsync(ct);

            return await GenerarNotaAsync(
                () => _ai.GenerarNotaClinicaGrupoAsync(grupo.Nombre, ingredientes, ct),
                $"grupo {id}");
        }

        // ─── Nota de PRECAUCIÓN de un GRUPO de riesgo (Anexo 5) ───────────────────────
        [HttpPost("grupo/{id:int}/generate-precaucion")]
        public async Task<IActionResult> GeneratePrecaucionGrupo(int id, CancellationToken ct)
        {
            var grupo = await _db.PlatGrupos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
            if (grupo == null) return NotFound(new { ok = false, error = "Grupo no encontrado" });
            if (string.IsNullOrWhiteSpace(grupo.Nombre))
                return BadRequest(new { ok = false, error = "El grupo no tiene nombre" });
            if (string.IsNullOrWhiteSpace(grupo.RiesgoTipo))
                return BadRequest(new { ok = false, error = "Este grupo no está marcado como grupo de riesgo. Asigna un tipo de riesgo en el editor del grupo antes de generar la precaución." });

            var ingredientes = await _db.PlatIngredientes.AsNoTracking()
                .Where(i => i.GrupoId == id)
                .OrderBy(i => i.Nombre)
                .Select(i => i.Nombre)
                .ToListAsync(ct);

            return await GenerarNotaAsync(
                () => _ai.GenerarNotaPrecaucionGrupoAsync(grupo.Nombre, grupo.RiesgoTipo, ingredientes, ct),
                $"precaución grupo {id}");
        }

        // ─── Nota clínica de INGREDIENTE ──────────────────────────────────────────────
        [HttpPost("ingrediente/{id:int}/generate-nota")]
        public async Task<IActionResult> GenerateNotaIngrediente(int id, CancellationToken ct)
        {
            var ing = await _db.PlatIngredientes.AsNoTracking()
                .Include(i => i.Grupo)
                .Include(i => i.Atributos).ThenInclude(a => a.Atributo)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
            if (ing == null) return NotFound(new { ok = false, error = "Ingrediente no encontrado" });
            if (string.IsNullOrWhiteSpace(ing.Nombre))
                return BadRequest(new { ok = false, error = "El ingrediente no tiene nombre" });

            var atributos = ing.Atributos
                .Where(a => a.Atributo != null)
                .Select(a => a.Atributo!.Nombre)
                .OrderBy(n => n)
                .ToList();

            return await GenerarNotaAsync(
                () => _ai.GenerarNotaClinicaIngredienteAsync(ing.Nombre, ing.Grupo?.Nombre, atributos, ct),
                $"ingrediente {id}");
        }

        // ─── NotasEII (texto corto) para GRUPO / INGREDIENTE ──────────────────────────
        [HttpPost("grupo/{id:int}/generate-notaseii")]
        public async Task<IActionResult> GenerateNotasEiiGrupo(int id, CancellationToken ct)
        {
            var grupo = await _db.PlatGrupos.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
            if (grupo == null) return NotFound(new { ok = false, error = "Grupo no encontrado" });
            if (string.IsNullOrWhiteSpace(grupo.Nombre))
                return BadRequest(new { ok = false, error = "El grupo no tiene nombre" });

            return await GenerarTextoAsync(
                () => _ai.GenerarNotasEiiAsync("el grupo", grupo.Nombre, ct), $"NotasEII grupo {id}");
        }

        [HttpPost("ingrediente/{id:int}/generate-notaseii")]
        public async Task<IActionResult> GenerateNotasEiiIngrediente(int id, CancellationToken ct)
        {
            var ing = await _db.PlatIngredientes.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
            if (ing == null) return NotFound(new { ok = false, error = "Ingrediente no encontrado" });
            if (string.IsNullOrWhiteSpace(ing.Nombre))
                return BadRequest(new { ok = false, error = "El ingrediente no tiene nombre" });

            return await GenerarTextoAsync(
                () => _ai.GenerarNotasEiiAsync("el ingrediente", ing.Nombre, ct), $"NotasEII ingrediente {id}");
        }

        // ─── Descripción TAXONÓMICA: atributo / categoría ─────────────────────────────
        [HttpPost("atributo/{id:int}/generate-descripcion")]
        public async Task<IActionResult> GenerateDescripcionAtributo(int id, CancellationToken ct)
        {
            var atr = await _db.PlatAtributos.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
            if (atr == null) return NotFound(new { ok = false, error = "Atributo no encontrado" });
            if (string.IsNullOrWhiteSpace(atr.Nombre))
                return BadRequest(new { ok = false, error = "El atributo no tiene nombre" });

            return await GenerarTextoAsync(
                () => _ai.GenerarDescripcionAtributoAsync(atr.Nombre, atr.Ambito, ct), $"atributo {id}");
        }

        [HttpPost("categoria/{id:int}/generate-descripcion")]
        public async Task<IActionResult> GenerateDescripcionCategoria(int id, CancellationToken ct)
        {
            var cat = await _db.PlatCategorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (cat == null) return NotFound(new { ok = false, error = "Categoría no encontrada" });
            if (string.IsNullOrWhiteSpace(cat.Nombre))
                return BadRequest(new { ok = false, error = "La categoría no tiene nombre" });

            return await GenerarTextoAsync(
                () => _ai.GenerarDescripcionCategoriaAsync(cat.Nombre, ct), $"categoría {id}");
        }

        // ─── Helpers de respuesta ─────────────────────────────────────────────────────

        private async Task<IActionResult> GenerarNotaAsync(
            Func<Task<NotaClinicaGeneradaDto>> generar, string ctx)
        {
            try
            {
                var nota = await generar();
                return Ok(new
                {
                    ok = true,
                    titulo = nota.Titulo,
                    queEs = nota.QueEs,
                    queSuelePasar = nota.QueSuelePasar,
                    importante = nota.Importante,
                    fuentes = nota.Fuentes,
                    revisionPrioritaria = nota.RevisionPrioritaria
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar nota clínica IA para {Ctx}", ctx);
                return StatusCode(500, new { ok = false, error = "Error al generar el contenido: " + ex.Message });
            }
        }

        private async Task<IActionResult> GenerarTextoAsync(Func<Task<string>> generar, string ctx)
        {
            try
            {
                var texto = await generar();
                return Ok(new { ok = true, texto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar texto IA para {Ctx}", ctx);
                return StatusCode(500, new { ok = false, error = "Error al generar el contenido: " + ex.Message });
            }
        }
    }
}
