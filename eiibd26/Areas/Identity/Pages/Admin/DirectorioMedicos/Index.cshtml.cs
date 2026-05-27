using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using eiibd26.Data;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Areas.Identity.Pages.Admin.DirectorioMedicos;

[Authorize(Roles = "Administrador")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly string _googleMapsApiKey;
    private readonly eiibd26.Services.Medico.IMedicoBadgeService _badgeService;

    public string GoogleMapsApiKey => _googleMapsApiKey;

    public IndexModel(ApplicationDbContext db, IConfiguration cfg, eiibd26.Services.Medico.IMedicoBadgeService badgeService)
    {
        _db = db;
        _googleMapsApiKey = cfg["GoogleMaps:ApiKey"] ?? string.Empty;
        _badgeService = badgeService;
    }

    public void OnGet() { }

    // ── Grid DataTable ───────────────────────────────────────────────────
    public async Task<IActionResult> OnGetGridDataAsync(
        bool mostrarEliminados = false, string? filtroVerificado = null)
    {
        var draw   = int.TryParse(Request.Query["draw"],   out var d) ? d : 1;
        var start  = int.TryParse(Request.Query["start"],  out var s) ? s : 0;
        var length = int.TryParse(Request.Query["length"], out var l) ? l : 25;
        var search = Request.Query["search[value]"].ToString();

        // FIX BUG-1: switch ON → solo eliminados; switch OFF → solo activos
        IQueryable<MedicoDirectorio> query = mostrarEliminados
            ? _db.MedicosDirectorio.IgnoreQueryFilters().AsNoTracking().Where(m => m.Eliminado)
            : _db.MedicosDirectorio.AsNoTracking().Where(m => !m.Eliminado);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m =>
                m.NombreCompleto.Contains(search) ||
                (m.Especialidad != null && m.Especialidad.Contains(search)) ||
                (m.Estado != null && m.Estado.Contains(search)) ||
                (m.Ciudad != null && m.Ciudad.Contains(search)));

        if (filtroVerificado == "verificado")
            query = query.Where(m => m.EstatusValidacion == EstatusValidacionCedula.Validado);
        else if (filtroVerificado == "pendiente")
            query = query.Where(m => m.EstatusValidacion != EstatusValidacionCedula.Validado);
        else if (filtroVerificado == "claim")
            query = query.Where(m => m.EstatusReclamacion == EstatusReclamacion.EnProceso);

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(m => m.FechaCreacion)
            .Skip(start).Take(length)
            .Select(m => new
            {
                m.Id, m.NombreCompleto,
                Especialidad    = m.Especialidad ?? string.Empty,
                Estado          = m.Estado       ?? string.Empty,
                Ciudad          = m.Ciudad       ?? string.Empty,
                m.FechaCreacion, m.NivelConfianza, m.Eliminado,
                Verificado      = m.EstatusValidacion == EstatusValidacionCedula.Validado,
                PropuestoPorId  = m.PropuestoPorUsuarioId,
                Confirmaciones  = _db.ConfirmacionesComunitarias.Count(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
                TieneConfEII    = _db.ConfirmacionesComunitarias.Any(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
                SolicitudClaim  = m.EstatusReclamacion == EstatusReclamacion.EnProceso,
                PerfilReclamado = m.EstatusReclamacion == EstatusReclamacion.Reclamado,
                EmailClaim      = m.EmailSolicitudClaim
            })
            .ToListAsync();

        var userGuids = rows.Where(r => r.PropuestoPorId.HasValue).Select(r => r.PropuestoPorId!.Value).Distinct().ToList();
        var nombres = userGuids.Count > 0
            ? await _db.Users.AsNoTracking().Where(u => userGuids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? "—")
            : new Dictionary<Guid, string>();

        var data = rows.Select(r => new
        {
            id                  = r.Id,
            nombreCompleto      = r.NombreCompleto,
            especialidad        = r.Especialidad,
            ubicacion           = string.Join(", ", new[] { r.Ciudad, r.Estado }.Where(x => !string.IsNullOrWhiteSpace(x))),
            fechaCreacion       = r.FechaCreacion.ToString("dd/MM/yyyy"),
            verificado          = r.Verificado,
            cedulaVerificada    = r.Verificado,
            nivelVerificacion   = (int)r.NivelConfianza,
            totalConfirmaciones = r.Confirmaciones,
            tieneConfirmacionEII = r.TieneConfEII,
            eliminado           = r.Eliminado,
            solicitudClaim      = r.SolicitudClaim,
            perfilReclamado     = r.PerfilReclamado,
            emailClaim          = r.EmailClaim ?? string.Empty,
            aportante           = r.PropuestoPorId.HasValue && nombres.TryGetValue(r.PropuestoPorId.Value, out var n) ? n : "—"
        });

        return new JsonResult(new { draw, recordsTotal = total, recordsFiltered = total, data });
    }

    // ── Catálogo de países para el panel ────────────────────────────────
    public async Task<IActionResult> OnGetPaisesAsync()
    {
        var paises = await _db.Paises
            .AsNoTracking()
            .Where(p => !p.Borrado && p.VIsibleBuscador)
            .OrderBy(p => p.PaisNombre)
            .Select(p => new { codigo = p.PaisCodigo, nombre = p.PaisNombre })
            .ToListAsync();
        return new JsonResult(paises);
    }

    // ── Cargar médico para el panel de edición ───────────────────────────
    public async Task<IActionResult> OnGetMedicoAsync(int id)
    {
        var m = await _db.MedicosDirectorio.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();

        // Contadores y listado desde ConfirmacionesComunitarias (fuente canónica)
        var confs = await _db.ConfirmacionesComunitarias.AsNoTracking()
            .Include(c => c.TipoConfirmacion)
            .Where(c => c.MedicoDirectorioId == id && !c.Eliminado)
            .ToListAsync();

        // Confirmadores — join con Users para email
        var userIds = confs.Select(c => c.UsuarioId).Distinct().ToList();
        var users = userIds.Count > 0
            ? await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "—")
            : new Dictionary<Guid, string>();

        // expContadores: mantiene estructura JSON; modelo nuevo no tiene campos por área
        var areas = new[] { "CUCI","Crohn","Pediátrico","Ostomías","Biológicos","Embarazo+EII","Manejo brotes","Segunda opinión","Cirugía","Seguimiento" };
        var expContadores = areas.Select(a => new { nombre = a, total = 0 }).ToList();

        var confirmadoresList = confs.OrderByDescending(c => c.FechaCreacion).Select(c => new
        {
            email = users.TryGetValue(c.UsuarioId, out var em) ? em : "—",
            fecha = c.FechaCreacion.ToString("dd/MM/yyyy"),
            exps  = c.TipoConfirmacion != null
                ? new List<string?> { c.TipoConfirmacion.Nombre }
                : new List<string?>()
        }).ToList();

        string? aportante = null;
        if (m.PropuestoPorUsuarioId.HasValue)
        {
            var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == m.PropuestoPorUsuarioId.Value);
            aportante = u?.UserName ?? u?.Email;
        }

        // Badges del médico
        var catalogo = await _db.MedicosBadge.AsNoTracking()
            .Where(b => b.Activo)
            .OrderBy(b => b.Orden)
            .ToListAsync();

        var ganadosIds = await _db.MedicosPerfilBadge.AsNoTracking()
            .Where(pb => pb.MedicoId == id)
            .Select(pb => new { pb.BadgeId, pb.FechaObtenido, pb.OtorgadoPor })
            .ToListAsync();

        var badgesData = catalogo.Select(b => {
            var ganado = ganadosIds.FirstOrDefault(g => g.BadgeId == b.Id);
            return new {
                id           = b.Id,
                codigo       = b.Codigo,
                nombre       = b.Nombre,
                nivel        = b.Nivel,
                icono        = b.Icono,
                obtenido     = ganado != null,
                fechaObtenido = ganado?.FechaObtenido.ToString("dd/MM/yyyy"),
                otorgadoPor  = ganado?.OtorgadoPor,
                esManual     = b.Codigo == "creador_contenido"  // FIX BUG-3: "verificado" se controla desde cédula, no desde badges
            };
        }).ToList();

        return new JsonResult(new
        {
            id = m.Id, nombreCompleto = m.NombreCompleto,
            especialidad = m.Especialidad ?? "", subespecialidad = m.Subespecialidad ?? "",
            cedulaProfesional = m.CedulaProfesional ?? "",
            nombrePais = m.NombrePais ?? "", ciudad = m.Ciudad ?? "",
            estado = m.Estado ?? "", hospitalClinica = m.HospitalClinica ?? "",
            nivelVerificacion = (int)m.NivelConfianza,
            cedulaVerificada = m.CedulaVerificada,
            fechaCedulaVerificada = m.FechaCedulaVerificada?.ToString("dd/MM/yyyy HH:mm"),
            activo = m.Activo,
            visible = m.VisiblePublicamente,
            totalConfirmaciones = confs.Count,
            tieneConfirmacionEII = confs.Any(),
            estatusReclamacion = m.EstatusReclamacion.ToString(),
            solicitudClaim = m.SolicitudClaimPendiente, perfilReclamado = m.PerfilReclamado,
            emailClaim = m.EmailSolicitudClaim ?? "", fechaReclamacion = m.FechaReclamacion?.ToString("dd/MM/yyyy"),
            eliminado = m.Eliminado, fechaCreacion = m.FechaCreacion.ToString("dd/MM/yyyy"),
            aportante = aportante ?? "—",
            expContadores,
            confirmadores = confirmadoresList,
            badges = badgesData
        });
    }

    // ── Guardar edición completa ─────────────────────────────────────────
    public async Task<IActionResult> OnPostEditarAsync(
        int id, string nombreCompleto, string? especialidad, string? subespecialidad,
        string? cedulaProfesional, string? nombrePais, string? ciudad, string? estado,
        string? hospitalClinica, bool cedulaVerificada, bool eliminado)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            return new JsonResult(new { ok = false, message = "El nombre es obligatorio." });

        var medico = await _db.MedicosDirectorio.FirstOrDefaultAsync(m => m.Id == id);
        if (medico is null) return new JsonResult(new { ok = false, message = "Médico no encontrado." });

        var cambioVerificacion = medico.CedulaVerificada != cedulaVerificada;

        medico.NombreCompleto    = nombreCompleto.Trim();
        medico.Especialidad      = especialidad?.Trim();
        medico.Subespecialidad   = subespecialidad?.Trim();
        medico.CedulaProfesional = cedulaProfesional?.Trim();
        medico.NombrePais        = nombrePais?.Trim();
        medico.Ciudad            = ciudad?.Trim();
        medico.Estado            = estado?.Trim();
        medico.HospitalClinica   = hospitalClinica?.Trim();
        medico.Eliminado         = eliminado;
        medico.FechaModificacion = DateTimeOffset.UtcNow;

        if (cambioVerificacion)
        {
            if (cedulaVerificada) { medico.EstatusValidacion = EstatusValidacionCedula.Validado; medico.FechaCedulaVerificada = DateTime.UtcNow; }
            else { medico.EstatusValidacion = EstatusValidacionCedula.PendienteValidacion; medico.FechaCedulaVerificada = null; }
        }

        await _db.SaveChangesAsync();

        if (cambioVerificacion)
        {
            // FIX BUG-3: badge "verificado" sigue a EstatusValidacion como única fuente canónica
            try
            {
                if (cedulaVerificada) await _badgeService.OtorgarBadgeAsync(medico.Id, "verificado", "admin");
                else await _badgeService.RevocarBadgeAsync(medico.Id, "verificado");
            }
            catch { }
            await RecalcularNivelAsync(medico, id);
        }

        return new JsonResult(new { ok = true });
    }

    // ── Handlers existentes ──────────────────────────────────────────────
    public async Task<IActionResult> OnPostVerificarAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        var toggleVerificado = m.EstatusValidacion != EstatusValidacionCedula.Validado;
        m.EstatusValidacion = toggleVerificado ? EstatusValidacionCedula.Validado : EstatusValidacionCedula.PendienteValidacion;
        m.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        // FIX BUG-3: sincronizar badge "verificado"
        try
        {
            if (toggleVerificado) await _badgeService.OtorgarBadgeAsync(m.Id, "verificado", "admin");
            else await _badgeService.RevocarBadgeAsync(m.Id, "verificado");
        }
        catch { }
        return new JsonResult(new { success = true, verificado = m.EstatusValidacion == EstatusValidacionCedula.Validado });
    }
    public async Task<IActionResult> OnPostEliminarAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        m.Eliminado = true; m.FechaModificacion = DateTimeOffset.UtcNow; await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostRestaurarAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        m.Eliminado = false; m.FechaModificacion = DateTimeOffset.UtcNow; await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostVerificarCedulaAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        m.EstatusValidacion = EstatusValidacionCedula.Validado; m.FechaCedulaVerificada = DateTime.UtcNow; m.FechaModificacion = DateTimeOffset.UtcNow;
        await RecalcularNivelAsync(m, id);
        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostAprobarClaimAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        m.EstatusReclamacion = EstatusReclamacion.Reclamado; m.FechaReclamacion = DateTimeOffset.UtcNow;
        m.NivelConfianza = NivelConfianzaEnum.Establecido; m.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostRechazarClaimAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        m.EstatusReclamacion = EstatusReclamacion.Rechazado; m.EmailSolicitudClaim = null; m.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostOtorgarBadgeAsync(int medicoId, string codigo)
    {
        var result = await _badgeService.OtorgarBadgeAsync(medicoId, codigo, "admin");
        return new JsonResult(new { success = result });
    }

    public async Task<IActionResult> OnPostEvaluarBadgesAsync(int medicoId)
    {
        await _badgeService.EvaluarBadgesAutomaticosAsync(medicoId);
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostEvaluarTodosAsync()
    {
        var ids = await _db.MedicosDirectorio.AsNoTracking()
            .Where(m => !m.Eliminado)
            .Select(m => m.Id)
            .ToListAsync();
        foreach (var id in ids)
            await _badgeService.EvaluarBadgesAutomaticosAsync(id);
        return new JsonResult(new { success = true, total = ids.Count });
    }

    public async Task<IActionResult> OnPostRevocarBadgeAsync(int medicoId, string codigo)
    {
        var badge = await _db.MedicosBadge.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Codigo == codigo);
        if (badge is null) return new JsonResult(new { success = false });

        var entry = await _db.MedicosPerfilBadge
            .FirstOrDefaultAsync(pb => pb.MedicoId == medicoId && pb.BadgeId == badge.Id);
        if (entry is null) return new JsonResult(new { success = false });

        _db.MedicosPerfilBadge.Remove(entry);
        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }

    private async Task RecalcularNivelAsync(MedicoDirectorio medico, int id)
    {
        var total    = await _db.ConfirmacionesComunitarias.CountAsync(c => c.MedicoDirectorioId == id && !c.Eliminado);
        var tieneEII = total > 0; // plataforma EII: cualquier confirmación implica experiencia EII
        var nivel = medico.PerfilReclamado ? 3
            : (medico.CedulaVerificada || total >= 5) ? 2
            : (total >= 3 && tieneEII) ? 1
            : 0;
        medico.NivelConfianza = (NivelConfianzaEnum)nivel;
        await _db.SaveChangesAsync();
    }
}
