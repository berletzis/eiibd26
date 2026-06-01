using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using eiibd26.Data;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;
using eiibd26.Models.Validacion;
using eiibd26.Services.Directorio;
using eiibd26.Services.Validacion;

namespace eiibd26.Areas.Identity.Pages.Admin.DirectorioMedicos;

[Authorize(Roles = "Administrador")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly string _googleMapsApiKey;
    private readonly eiibd26.Services.Medico.IMedicoBadgeService _badgeService;
    private readonly IMedicoDirectorioService _dirService;
    private readonly IValidacionContenidoService _validacionService;

    public string GoogleMapsApiKey => _googleMapsApiKey;

    public IndexModel(ApplicationDbContext db, IConfiguration cfg,
        eiibd26.Services.Medico.IMedicoBadgeService badgeService,
        IMedicoDirectorioService dirService,
        IValidacionContenidoService validacionService)
    {
        _db = db;
        _googleMapsApiKey = cfg["GoogleMaps:ApiKey"] ?? string.Empty;
        _badgeService = badgeService;
        _dirService = dirService;
        _validacionService = validacionService;
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
        var confs = await _db.ConfirmacionesComunitarias.IgnoreQueryFilters().AsNoTracking()
            .Include(c => c.TipoConfirmacion)
            .Where(c => c.MedicoDirectorioId == id)
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
            id = c.Id,
            email = users.TryGetValue(c.UsuarioId, out var em) ? em : "—",
            fecha = c.FechaCreacion.ToString("dd/MM/yyyy"),
            exps  = c.TipoConfirmacion != null
                ? new List<string?> { c.TipoConfirmacion.Nombre }
                : new List<string?>(),
            eliminado = c.Eliminado,
            enRevision = c.Eliminado  // alias para claridad en UI
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
            .Select(pb => new { pb.BadgeId, pb.FechaObtenido, pb.OtorgadoPor, pb.EnRevision, pb.RevisionMotivo })
            .ToListAsync();

        var badgesData = catalogo.Select(b => {
            var ganado = ganadosIds.FirstOrDefault(g => g.BadgeId == b.Id);
            return new {
                id             = b.Id,
                codigo         = b.Codigo,
                nombre         = b.Nombre,
                nivel          = b.Nivel,
                icono          = b.Icono,
                obtenido       = ganado != null,
                fechaObtenido  = ganado?.FechaObtenido.ToString("dd/MM/yyyy"),
                otorgadoPor    = ganado?.OtorgadoPor,
                esManual       = b.Codigo == "creador_contenido",  // FIX BUG-3: "verificado" se controla desde cédula
                enRevision     = ganado?.EnRevision ?? false,
                revisionMotivo = ganado?.RevisionMotivo
            };
        }).ToList();

        // Validaciones de contenido vinculadas al médico (si tiene perfil reclamado)
        List<ValidacionAdminDto> validacionesContenido = new();
        try
        {
            var perfil = await _db.MedicosPerfilExtendido.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MedicoId == id && p.UserId != null);
            if (perfil?.UserId != null)
                validacionesContenido = await _validacionService.ObtenerValidacionesMedicoAsync(
                    perfil.UserId.Value.ToString());
        }
        catch { }

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
            totalConfirmaciones = confs.Count(c => !c.Eliminado),
            totalConfirmacionesIncRevision = confs.Count,
            tieneConfirmacionEII = confs.Any(c => !c.Eliminado),
            estatusReclamacion = m.EstatusReclamacion.ToString(),
            solicitudClaim = m.SolicitudClaimPendiente, perfilReclamado = m.PerfilReclamado,
            emailClaim = m.EmailSolicitudClaim ?? "", fechaReclamacion = m.FechaReclamacion?.ToString("dd/MM/yyyy"),
            eliminado = m.Eliminado, fechaCreacion = m.FechaCreacion.ToString("dd/MM/yyyy"),
            aportante = aportante ?? "—",
            expContadores,
            confirmadores = confirmadoresList,
            badges = badgesData,
            validacionesContenido = validacionesContenido.Select(v => new {
                id            = v.Id,
                tipo          = v.TipoContenido.ToString(),
                contenidoTitulo = v.ContenidoTitulo,
                contenidoUrl  = v.ContenidoUrl,
                comentario    = v.Comentario,
                estado        = v.Estado.ToString(),
                estadoNum     = (int)v.Estado,
                creadoEn      = v.CreadoEn.ToString("dd/MM/yyyy"),
                notaModeracion = v.NotaModeracion
            }).ToList(),
            badgeHistorial = (await _badgeService.GetHistorialAsync(id)).Select(h => new {
                badge  = h.BadgeCodigo,
                nombre = h.BadgeNombre,
                evento = h.Evento,
                actor  = h.Actor,
                motivo = h.Motivo ?? "",
                fecha  = h.FechaEvento.ToString("dd/MM/yyyy HH:mm")
            }).ToList()
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
                else await _badgeService.RevocarBadgeAsync(medico.Id, "verificado", "admin");
            }
            catch { }
            await _dirService.RecalcularNivelConfianzaAsync(medico.Id);
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
        await _db.SaveChangesAsync();
        // D-01: sincronizar badge verificado y recalcular nivel desde el servicio canónico
        try { await _badgeService.OtorgarBadgeAsync(id, "verificado", "admin"); } catch { }
        await _dirService.RecalcularNivelConfianzaAsync(id);
        return new JsonResult(new { success = true });
    }
    public async Task<IActionResult> OnPostAprobarClaimAsync()
    {
        if (!Request.HasFormContentType || string.IsNullOrWhiteSpace(Request.Form["id"])) return BadRequest();
        var id = int.Parse(Request.Form["id"]!);
        var m = await _db.MedicosDirectorio.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return new JsonResult(new { success = false });
        // D-02: no hardcodear NivelConfianza — delegar al servicio canónico
        m.EstatusReclamacion = EstatusReclamacion.Reclamado; m.FechaReclamacion = DateTimeOffset.UtcNow; m.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        await _dirService.RecalcularNivelConfianzaAsync(id);
        // D-02: evaluar badges automáticos ahora que el claim fue aprobado
        try { await _badgeService.EvaluarBadgesAutomaticosAsync(id); } catch { }
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
        // D-04: delegar al servicio (registra trazabilidad en historial)
        var result = await _badgeService.RevocarBadgeAsync(medicoId, codigo, "admin");
        return new JsonResult(new { success = result });
    }

    public async Task<IActionResult> OnPostMarcarRevisionBadgeAsync(int medicoId, string? codigo, bool enRevision, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return new JsonResult(new { success = false, message = "Código de badge requerido." });
        var result = await _badgeService.MarcarEnRevisionAsync(medicoId, codigo, enRevision, "admin", motivo);
        return new JsonResult(new { success = result });
    }

    public async Task<IActionResult> OnPostToggleConfirmacionAsync(int confirmacionId)
    {
        var conf = await _db.ConfirmacionesComunitarias.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == confirmacionId);

        if (conf is null)
            return new JsonResult(new { success = false, message = "Confirmación no encontrada." });

        // Toggle del estado: si está eliminado (en revisión), activar; si está activo, poner en revisión
        conf.Eliminado = !conf.Eliminado;
        await _db.SaveChangesAsync();

        // Recalcular el nivel de confianza del médico porque cambió el conteo de confirmaciones activas
        await _dirService.RecalcularNivelConfianzaAsync(conf.MedicoDirectorioId);

        // Re-evaluar badges automáticos (badge comunidad podría cambiar con el nuevo conteo)
        try { await _badgeService.EvaluarBadgesAutomaticosAsync(conf.MedicoDirectorioId); } catch { }

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostCambiarEstadoValidacionAsync(
        int? validacionId,
        string? nuevoEstado,
        string? nota)
    {
        if (validacionId == null || string.IsNullOrWhiteSpace(nuevoEstado))
            return new JsonResult(new { success = false, message = "Parámetros inválidos." });

        if (!Enum.TryParse<EstadoValidacion>(nuevoEstado, ignoreCase: true, out var estado))
            return new JsonResult(new { success = false, message = "Estado no válido." });

        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

        var ok = await _validacionService.CambiarEstadoAsync(validacionId.Value, estado, adminId, nota);
        return new JsonResult(new { success = ok });
    }
}


