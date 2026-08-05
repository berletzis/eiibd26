using System.Security.Claims;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Medico;

// MedicoPendiente incluido: el profesional pendiente aterriza aquí tras registrarse. Ver su panel
// es inofensivo; las acciones de validar/responder tienen su propio gating por rol/nivel.
[Authorize(Roles = "Medico,MedicoPendiente")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IMedicoBadgeService _badgeService;
    private readonly ILogger<DashboardModel> _logger;

    public DashboardModel(ApplicationDbContext db, IMedicoBadgeService badgeService, ILogger<DashboardModel> logger)
    {
        _db = db;
        _badgeService = badgeService;
        _logger = logger;
    }

    public int NivelActual { get; set; }
    public List<MedicoBadgeDto> TodosLosBadges { get; set; } = new();
    public string? NombreMedico { get; set; }
    public string? FotoUrl { get; set; }
    public int? MedicoDirectorioId { get; set; }
    public bool TienePerfilVinculado { get; set; }
    /// <summary>Tipo declarado en la ficha — solo cambia el copy del CTA de validar.
    /// Null = general: texto genérico y TOP clínico, el comportamiento de siempre.</summary>
    public eiibd26.Models.Directorio.Enums.TipoProfesional? TipoProfesional { get; set; }
    public int TotalRecomendaciones { get; set; }
    public List<RecomendacionDashboardVm> Recomendaciones { get; set; } = new();
    public List<QaMedicoTopItem> QaTop5 { get; set; } = new();

    // Perfil huérfano: existe en directorio con AspNetUserId pero MedicoPerfilExtendido.MedicoId es NULL
    public int? PerfilHuerfanoId { get; set; }
    public string? PerfilHuerfanoNombre { get; set; }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = GetUserId();

        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .Include(p => p.Medico)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is not null)
        {
            FotoUrl              = perfil.Foto;
            TienePerfilVinculado = perfil.MedicoId.HasValue;
            MedicoDirectorioId   = perfil.MedicoId;
            NombreMedico         = perfil.Medico?.NombreCompleto;
            TipoProfesional      = perfil.TipoProfesional;

            // Si NO tiene vínculo, buscar si existe un perfil huérfano en el directorio
            if (!perfil.MedicoId.HasValue)
            {
                var perfilHuerfano = await _db.MedicosDirectorio
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m =>
                        m.AspNetUserId == userId &&
                        m.EstatusReclamacion == eiibd26.Models.Directorio.Enums.EstatusReclamacion.Reclamado &&
                        !m.Eliminado);

                if (perfilHuerfano is not null)
                {
                    PerfilHuerfanoId = perfilHuerfano.Id;
                    PerfilHuerfanoNombre = perfilHuerfano.NombreCompleto;
                }
            }

            if (perfil.MedicoId.HasValue)
            {
                NivelActual    = await _badgeService.GetNivelActualAsync(perfil.MedicoId.Value);
                TodosLosBadges = await _badgeService.GetTodosLosBadgesAsync(perfil.MedicoId.Value);

                TotalRecomendaciones = await _db.ConfirmacionesComunitarias
                    .AsNoTracking()
                    .CountAsync(c => c.MedicoDirectorioId == perfil.MedicoId.Value && !c.Eliminado);

                if (NivelActual >= 2)
                {
                    var confirmaciones = await _db.ConfirmacionesComunitarias
                        .AsNoTracking()
                        .Where(c => c.MedicoDirectorioId == perfil.MedicoId.Value && !c.Eliminado)
                        .OrderByDescending(c => c.FechaCreacion)
                        .Take(20)
                        .ToListAsync();

                    Dictionary<Guid, string> nombresPacientes = new();
                    if (NivelActual >= 3)
                    {
                        var userIds = confirmaciones.Select(c => c.UsuarioId).Distinct().ToList();
                        nombresPacientes = await _db.Perfil
                            .AsNoTracking()
                            .Where(p => userIds.Contains(p.idUser) && p.PermitirCompartirDatosMedicos == true)
                            .ToDictionaryAsync(
                                p => p.idUser,
                                p => ($"{p.Nombre} {p.Apellidos}").Trim());
                    }

                    Recomendaciones = confirmaciones.Select(c => new RecomendacionDashboardVm
                    {
                        FechaConfirmacion = c.FechaCreacion.DateTime,
                        ExpCUCI           = false,
                        ExpCrohn          = false,
                        ExpPediatrico     = false,
                        ExpBiologicos     = false,
                        NombrePaciente    = NivelActual >= 3 && nombresPacientes.TryGetValue(c.UsuarioId, out var n)
                                               ? n : null
                    }).ToList();
                }

                if (NivelActual >= 4)
                {
                    QaTop5 = await _db.Preguntas
                        .AsNoTracking()
                        .Where(p => !p.Eliminado &&
                                    _db.Respuestas.Any(r => r.PreguntaId == p.Id &&
                                                            r.UsuarioId == userId &&
                                                            !r.Eliminado))
                        .Select(p => new QaMedicoTopItem
                        {
                            Id             = p.Id,
                            Titulo         = p.Titulo,
                            Slug           = p.Slug,
                            RespuestasCount = _db.Respuestas.Count(r => r.PreguntaId == p.Id && !r.Eliminado),
                            Score          = _db.Votos
                                                .Where(v => v.EntidadTipo == eiibd26.Controllers.VotoTipo.Pregunta
                                                            && v.EntidadId == p.Id
                                                            && !v.Eliminado)
                                                .Select(v => (int?)v.Valor).Sum() ?? 0,
                            FechaCreacion  = p.FechaCreacion
                        })
                        .OrderByDescending(q =>
                            (double)(q.Score * 2 + q.RespuestasCount) /
                            (2.0 + EF.Functions.DateDiffDay(q.FechaCreacion, DateTime.UtcNow)))
                        .Take(5)
                        .ToListAsync();
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRevincularPerfilAsync()
    {
        var userId = GetUserId();

        // Buscar perfil extendido del usuario
        var perfil = await _db.MedicosPerfilExtendido
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (perfil is null)
        {
            TempData["Error"] = "No se encontró tu perfil extendido. Contacta al soporte.";
            return RedirectToPage();
        }

        // Buscar perfil huérfano en el directorio
        var perfilHuerfano = await _db.MedicosDirectorio
            .FirstOrDefaultAsync(m =>
                m.AspNetUserId == userId &&
                m.EstatusReclamacion == eiibd26.Models.Directorio.Enums.EstatusReclamacion.Reclamado &&
                !m.Eliminado);

        if (perfilHuerfano is null)
        {
            TempData["Error"] = "No se encontró un perfil del directorio asociado a tu cuenta.";
            return RedirectToPage();
        }

        // Restaurar vínculo
        perfil.MedicoId = perfilHuerfano.Id;
        perfil.FechaModificado = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Perfil re-vinculado automáticamente: UserId={UserId}, MedicoId={MedicoId}, Nombre={Nombre}",
            userId, perfilHuerfano.Id, perfilHuerfano.NombreCompleto);

        TempData["Success"] = $"Tu perfil '{perfilHuerfano.NombreCompleto}' fue re-vinculado exitosamente.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSolicitarContenidoAsync(string? tituloSolicitud)
    {
        if (string.IsNullOrWhiteSpace(tituloSolicitud))
        {
            TempData["Error"] = "El título de la solicitud es requerido.";
            return RedirectToPage();
        }

        _logger.LogInformation("Médico {UserId} solicita crear contenido: {Titulo}",
            GetUserId(), tituloSolicitud);

        TempData["Success"] = "Solicitud enviada. El equipo EIIBD la revisará pronto.";
        return RedirectToPage();
    }
}

public class QaMedicoTopItem
{
    public Guid    Id              { get; set; }
    public string  Titulo          { get; set; } = "";
    public string? Slug            { get; set; }
    public int     RespuestasCount { get; set; }
    public int     Score           { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}

public class RecomendacionDashboardVm
{
    public DateTime FechaConfirmacion { get; set; }
    public bool ExpCUCI { get; set; }
    public bool ExpCrohn { get; set; }
    public bool ExpPediatrico { get; set; }
    public bool ExpBiologicos { get; set; }
    public string? NombrePaciente { get; set; }
}
