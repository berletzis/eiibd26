using System.Security.Claims;
using eiibd26.Models.Medico;
using eiibd26.Services.Medico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Areas.Identity.Pages.Medico;

[Authorize(Roles = "Medico")]
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
    public int TotalRecomendaciones { get; set; }
    public List<RecomendacionDashboardVm> Recomendaciones { get; set; } = new();
    public string QaUrl { get; set; } = "/Preguntas/Index";

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

                // Construir URL Q&A con filtro de áreas EII del médico
                var areasIds = await _db.MedicoAreasEii
                    .AsNoTracking()
                    .Where(a => a.MedicoPerfilId == perfil.Id)
                    .Select(a => a.CondicionId)
                    .ToListAsync();

                if (areasIds.Any())
                {
                    var nombresAreas = await _db.condiciones
                        .AsNoTracking()
                        .Where(c => areasIds.Contains(c.id))
                        .Select(c => c.nombre)
                        .ToListAsync();

                    if (nombresAreas.Any())
                    {
                        var areasParam = string.Join(",", nombresAreas
                            .Select(n => Uri.EscapeDataString(n ?? "")));
                        QaUrl = $"/Preguntas/Index?areas={areasParam}";
                    }
                }

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

    public async Task<IActionResult> OnPostSolicitarContenidoAsync(string tituloSolicitud)
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

public class RecomendacionDashboardVm
{
    public DateTime FechaConfirmacion { get; set; }
    public bool ExpCUCI { get; set; }
    public bool ExpCrohn { get; set; }
    public bool ExpPediatrico { get; set; }
    public bool ExpBiologicos { get; set; }
    public string? NombrePaciente { get; set; }
}
