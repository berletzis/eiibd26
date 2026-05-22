using eiibd26.Data;
using eiibd26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Areas.Identity.Pages.Usuario
{
    [Authorize(Roles = "Paciente,Administrador")]
    [IgnoreAntiforgeryToken]
    public class UsuarioLaboratoriosModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UsuarioLaboratoriosModel> _logger;

        public UsuarioLaboratoriosModel(ApplicationDbContext db, ILogger<UsuarioLaboratoriosModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        public class ResultadoVm
        {
            public int Id { get; set; }
            public string TipoPadre { get; set; } = "";
            public string TipoNombre { get; set; } = "";
            public string? ResultValue { get; set; }
            public string? ResultUnit { get; set; }
            public string? Notes { get; set; }
            public DateTime? ResultDate { get; set; }
            public DateTime FechaCreado { get; set; }
            public int? CondicionUsuarioId { get; set; }
            public string CondicionNombre { get; set; } = "";
            public int? SintomaUsuarioId { get; set; }
            public string SintomaNombre { get; set; } = "";
            public int? TratamientoUsuarioId { get; set; }
            public string TratamientoNombre { get; set; } = "";
            public int? LaboratoryUnitCatalogId { get; set; }
            public string UnidadAbreviatura { get; set; } = "";
        }

        public class TipoHojaVm { public int Id { get; set; } public string Display { get; set; } = ""; }
        public class RelacionVm   { public int Id { get; set; } public string Nombre { get; set; } = ""; }
        public class UnidadVm     { public int Id { get; set; } public string Nombre { get; set; } = ""; public string Abreviatura { get; set; } = ""; }

        public List<ResultadoVm> Resultados { get; set; } = new();
        public List<TipoHojaVm> TiposHoja { get; set; } = new();
        public List<RelacionVm> MisCondiciones { get; set; } = new();
        public List<RelacionVm> MisSintomas { get; set; } = new();
        public List<RelacionVm> MisTratamientos { get; set; } = new();
        public List<UnidadVm> UnidadesMedida { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (User.IsInRole("Medico")) { Response.Redirect("/Identity/Medico/Dashboard"); return; }
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return;

            var userGuid = Guid.Parse(userId);
            await CargarTiposHojaAsync();
            await CargarUnidadesAsync();
            await CargarRelacionesAsync(userGuid);
            await CargarResultadosAsync(userGuid);
        }

        private async Task CargarTiposHojaAsync()
        {
            var allTypes = await _db.LaboratoryTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .Select(t => new { t.Id, t.Name, t.ParentId })
                .ToListAsync();

            var idsConHijos = allTypes
                .Where(t => t.ParentId.HasValue)
                .Select(t => t.ParentId!.Value)
                .ToHashSet();

            TiposHoja = allTypes
                .Where(t => t.ParentId.HasValue && !idsConHijos.Contains(t.Id))
                .Select(t =>
                {
                    var padre = allTypes.FirstOrDefault(p => p.Id == t.ParentId!.Value);
                    return new TipoHojaVm
                    {
                        Id      = t.Id,
                        Display = $"{padre?.Name ?? ""} - {t.Name}"
                    };
                })
                .OrderBy(t => t.Display)
                .ToList();
        }

        private async Task CargarUnidadesAsync()
        {
            UnidadesMedida = await _db.LaboratoryUnitCatalog
                .AsNoTracking()
                .OrderBy(u => u.Orden)
                .Select(u => new UnidadVm { Id = u.Id, Nombre = u.Nombre, Abreviatura = u.Abreviatura })
                .ToListAsync();
        }

        private async Task CargarRelacionesAsync(Guid userId)
        {
            MisCondiciones = await (from cu in _db.condicionUsuario
                                    join c in _db.condiciones on cu.idCondicion equals c.id
                                    where cu.idUsuario == userId && !cu.Eliminado
                                    orderby c.nombre
                                    select new RelacionVm { Id = cu.id, Nombre = c.nombre ?? "" })
                                   .ToListAsync();

            MisSintomas = await (from su in _db.sintomasUsuario
                                 join s in _db.sintomas on su.idSintoma equals s.id
                                 where su.idUsuario == userId && !su.Eliminado
                                 orderby s.nombre
                                 select new RelacionVm { Id = su.id, Nombre = s.nombre ?? "" })
                                .ToListAsync();

            MisTratamientos = await (from tu in _db.tratamientoUsuario
                                     join t in _db.tratamientos on tu.idTratamiento equals t.id
                                     where tu.idUsuario == userId && !tu.Eliminado
                                     orderby t.nombre
                                     select new RelacionVm { Id = tu.id, Nombre = t.nombre ?? "" })
                                    .ToListAsync();
        }

        private async Task CargarResultadosAsync(Guid userId)
        {
            var raw = await _db.PatientLaboratoryResults
                .AsNoTracking()
                .Where(r => r.PatientId == userId)
                .Include(r => r.LaboratoryType).ThenInclude(t => t.Parent)
                .Include(r => r.LaboratoryUnit)
                .Include(r => r.CondicionUsuario).ThenInclude(cu => cu!.Condicion)
                .Include(r => r.SintomaUsuario).ThenInclude(su => su!.Sintoma)
                .Include(r => r.TratamientoUsuario).ThenInclude(tu => tu!.Tratamiento)
                .OrderByDescending(r => r.ResultDate ?? (DateTime?)r.FechaCreado)
                .ToListAsync();

            Resultados = raw.Select(r => new ResultadoVm
            {
                Id                   = r.Id,
                TipoPadre            = r.LaboratoryType?.Parent?.Name ?? r.LaboratoryType?.Name ?? "",
                TipoNombre           = r.LaboratoryType?.Name ?? "",
                ResultValue          = r.ResultValue,
                ResultUnit           = r.ResultUnit,
                Notes                = r.Notes,
                ResultDate           = r.ResultDate,
                FechaCreado          = r.FechaCreado,
                CondicionUsuarioId   = r.CondicionUsuarioId,
                CondicionNombre      = r.CondicionUsuario?.Condicion?.nombre ?? "",
                SintomaUsuarioId     = r.SintomaUsuarioId,
                SintomaNombre        = r.SintomaUsuario?.Sintoma?.nombre ?? "",
                TratamientoUsuarioId = r.TratamientoUsuarioId,
                TratamientoNombre        = r.TratamientoUsuario?.Tratamiento?.nombre ?? "",
                LaboratoryUnitCatalogId  = r.LaboratoryUnitCatalogId,
                UnidadAbreviatura        = r.LaboratoryUnit?.Abreviatura ?? r.ResultUnit ?? ""
            }).ToList();
        }

        // POST: Agregar — solo necesita el tipo, los datos se completan en el card
        public async Task<IActionResult> OnPostAgregarResultadoAsync(int laboratoryTypeId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            try
            {
                var tieneHijos = await _db.LaboratoryTypes.AnyAsync(t => t.ParentId == laboratoryTypeId);
                if (tieneHijos)
                    return new JsonResult(new { ok = false, mensaje = "Selecciona una categoría específica." }) { StatusCode = 400 };

                var tipoExiste = await _db.LaboratoryTypes.AnyAsync(t => t.Id == laboratoryTypeId && t.IsActive);
                if (!tipoExiste)
                    return new JsonResult(new { ok = false, mensaje = "Tipo no encontrado." }) { StatusCode = 400 };

                var nuevo = new PatientLaboratoryResult
                {
                    PatientId        = Guid.Parse(userId),
                    LaboratoryTypeId = laboratoryTypeId,
                    FechaCreado      = DateTime.Now,
                    Eliminado        = false
                };

                _db.PatientLaboratoryResults.Add(nuevo);
                await _db.SaveChangesAsync();
                return new JsonResult(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar resultado de laboratorio. userId={UserId} tipoId={TipoId}", userId, laboratoryTypeId);
                return new JsonResult(new { ok = false, mensaje = "Error al guardar. Intenta de nuevo." }) { StatusCode = 500 };
            }
        }

        // POST: Actualizar datos del card
        public async Task<IActionResult> OnPostActualizarResultadoAsync(
            int resultadoId, string resultValue, string resultUnit, string notes, string resultDate,
            int? condicionUsuarioId, int? sintomaUsuarioId, int? tratamientoUsuarioId,
            int? laboratoryUnitCatalogId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var r = await _db.PatientLaboratoryResults
                .FirstOrDefaultAsync(x => x.Id == resultadoId && x.PatientId == Guid.Parse(userId) && !x.Eliminado);

            if (r == null) return BadRequest();

            r.ResultValue        = string.IsNullOrWhiteSpace(resultValue) ? null : resultValue.Trim();
            r.ResultUnit         = string.IsNullOrWhiteSpace(resultUnit)  ? null : resultUnit.Trim();
            r.Notes              = string.IsNullOrWhiteSpace(notes)       ? null : notes.Trim();
            r.ResultDate         = DateTime.TryParse(resultDate, out var d) ? d : null;
            r.CondicionUsuarioId      = condicionUsuarioId;
            r.SintomaUsuarioId        = sintomaUsuarioId;
            r.TratamientoUsuarioId    = tratamientoUsuarioId;
            r.LaboratoryUnitCatalogId = laboratoryUnitCatalogId;
            // ResultUnit se actualiza con la abreviatura del catálogo para compatibilidad
            if (laboratoryUnitCatalogId.HasValue)
            {
                var unidad = await _db.LaboratoryUnitCatalog.FindAsync(laboratoryUnitCatalogId.Value);
                r.ResultUnit = unidad?.Abreviatura;
            }
            else
            {
                r.ResultUnit = string.IsNullOrWhiteSpace(resultUnit) ? null : resultUnit.Trim();
            }
            r.FechaModificado = DateTime.Now;

            await _db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        // POST: Eliminar (soft delete)
        public async Task<IActionResult> OnPostEliminarResultadoAsync(int resultadoId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var r = await _db.PatientLaboratoryResults
                .FirstOrDefaultAsync(x => x.Id == resultadoId && x.PatientId == Guid.Parse(userId));

            if (r != null)
            {
                r.Eliminado      = true;
                r.FechaEliminado = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
