using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Services.Directorio;
using eiibd26.Services.Medico;
using eiibd26.Models.Directorio;
using eiibd26.Models.Medico;

namespace eiibd26.Pages.DirectorioMedicos;

public class DetalleModel : PageModel
{
    private readonly IMedicoDirectorioService _service;
    private readonly ApplicationDbContext _db;
    private readonly IMedicoBadgeService _badgeService;

    public DetalleModel(IMedicoDirectorioService service, ApplicationDbContext db, IMedicoBadgeService badgeService)
    {
        _service = service;
        _db = db;
        _badgeService = badgeService;
    }

    public MedicoDetalleVm? Medico { get; set; }
    public List<TipoConfirmacion> TiposConfirmacion { get; set; } = new();

    [BindProperty]
    public ConfirmarAtencionVm ConfirmarVm { get; set; } = new();

    // Confirmación simple (nueva tabla DirectorioMedicoConfirmacion)
    public bool YaConfirme { get; set; }
    public int TotalConfirmaciones { get; set; }
    public bool PerfilYaVinculado { get; set; }

    public bool IsPaciente { get; private set; }
    public bool IsMedico { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsOwnerMedico { get; private set; }
    public bool CanInteractAsPaciente { get; private set; }

    public List<UbicacionMedicoVm> UbicacionesCombinadas { get; private set; } = new();
    public List<MedicoBadgeDto> BadgesDirectorio { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        Medico = await _service.GetDetalleAsync(id, usuarioId);
        if (Medico is null) return NotFound();

        IsMedico   = User.IsInRole("Medico");
        IsPaciente = User.IsInRole("Paciente");
        IsAdmin    = User.IsInRole("Administrador");

        if (IsMedico && usuarioId.HasValue)
        {
            IsOwnerMedico = await _db.MedicosPerfilExtendido
                .AnyAsync(p => p.MedicoId == id && p.UserId == usuarioId.Value);
        }

        CanInteractAsPaciente = (IsPaciente || IsAdmin) && !IsMedico;

        TiposConfirmacion = await _service.GetTiposConfirmacionActivosAsync();
        ConfirmarVm.MedicoDirectorioId = id;

        TotalConfirmaciones = await _db.ConfirmacionesComunitarias
            .CountAsync(c => c.MedicoDirectorioId == id && !c.Eliminado);

        PerfilYaVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == id && p.UserId != null);

        if (usuarioId.HasValue)
            YaConfirme = await _db.ConfirmacionesComunitarias
                .AnyAsync(c => c.MedicoDirectorioId == id && c.UsuarioId == usuarioId.Value && !c.Eliminado);

        // Fase 2: Ubicaciones combinadas
        await CargarUbicacionesAsync(id);

        // Fase 4: Badges del directorio
        BadgesDirectorio = await _badgeService.GetTodosLosBadgesAsync(id);

        return Page();
    }

    // Handler existente para TipoConfirmacion
    public async Task<IActionResult> OnPostConfirmarAsync()
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var usuarioId = ObtenerUsuarioId()!.Value;
        var resultado = await _service.ConfirmarAtencionAsync(
            ConfirmarVm.MedicoDirectorioId, ConfirmarVm.TipoConfirmacionId, usuarioId);

        TempData[resultado ? "Success" : "Error"] = resultado
            ? "Tu confirmación fue registrada. Gracias por contribuir al directorio comunitario."
            : "Ya registraste este tipo de confirmación para este médico.";

        if (resultado)
        {
            await _badgeService.EvaluarBadgesAutomaticosAsync(ConfirmarVm.MedicoDirectorioId);
        }

        return RedirectToPage(new { id = ConfirmarVm.MedicoDirectorioId });
    }

    // Handler de confirmación simple (nueva tabla ConfirmacionComunitaria)
    public async Task<IActionResult> OnPostConfirmarSimpleAsync(int medicoId)
    {
        if (!User.Identity!.IsAuthenticated)
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var usuarioId = ObtenerUsuarioId()!.Value;

        var existe = await _db.ConfirmacionesComunitarias
            .AnyAsync(c => c.MedicoDirectorioId == medicoId && c.UsuarioId == usuarioId && !c.Eliminado);

        if (existe)
        {
            TempData["Error"] = "Ya confirmaste a este médico anteriormente.";
            return RedirectToPage(new { id = medicoId });
        }

        var tipoConfirmacion = await _db.TiposConfirmacion
            .OrderBy(t => t.Orden)
            .FirstOrDefaultAsync(t => t.Activo);

        if (tipoConfirmacion is null)
        {
            TempData["Error"] = "No hay tipos de confirmación disponibles.";
            return RedirectToPage(new { id = medicoId });
        }

        _db.ConfirmacionesComunitarias.Add(new ConfirmacionComunitaria
        {
            MedicoDirectorioId = medicoId,
            UsuarioId          = usuarioId,
            TipoConfirmacionId = tipoConfirmacion.Id,
            FechaCreacion      = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        await _service.RecalcularNivelConfianzaAsync(medicoId);
        try
        {
            await _badgeService.EvaluarBadgesAutomaticosAsync(medicoId);
        }
        catch (Exception ex)
        {
            _ = ex; // badge evaluation failure must not block the save
        }

        TempData["Success"] = "¡Gracias! Tu confirmación ayuda a la comunidad EII.";
        return RedirectToPage(new { id = medicoId });
    }

    private async Task CargarUbicacionesAsync(int medicoId)
    {
        var ubicaciones = new List<UbicacionMedicoVm>();

        // Hospitales ingresados por el médico en su perfil (campo texto libre)
        var perfil = await _db.MedicosPerfilExtendido
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MedicoId == medicoId);

        if (perfil != null && !string.IsNullOrWhiteSpace(perfil.Hospitales))
        {
            IEnumerable<string> hospitalesMedico;
            try
            {
                hospitalesMedico = System.Text.Json.JsonSerializer.Deserialize<List<string>>(perfil.Hospitales)
                    ?? Enumerable.Empty<string>();
            }
            catch
            {
                // Fallback para datos en formato texto plano (legacy)
                hospitalesMedico = perfil.Hospitales
                    .Split(new[] { '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrWhiteSpace(h));
            }

            foreach (var hospital in hospitalesMedico)
            {
                ubicaciones.Add(new UbicacionMedicoVm
                {
                    Hospital = hospital,
                    Ciudad   = perfil.Ciudad ?? Medico!.Ciudad ?? "",
                    Estado   = perfil.Estado ?? Medico!.Estado ?? "",
                    Pais     = perfil.PaisCodigo ?? "",
                    Fuente   = "medico",
                    Reportes = 1
                });
            }
        }

        // Hospital del directorio (reportado por la comunidad al proponer al médico)
        if (!string.IsNullOrWhiteSpace(Medico!.HospitalClinica))
        {
            var yaExiste = ubicaciones.Any(u =>
                string.Equals(u.Hospital, Medico.HospitalClinica, StringComparison.OrdinalIgnoreCase));

            if (!yaExiste)
            {
                ubicaciones.Add(new UbicacionMedicoVm
                {
                    Hospital = Medico.HospitalClinica,
                    Ciudad   = Medico.Ciudad ?? "",
                    Estado   = Medico.Estado ?? "",
                    Fuente   = "paciente",
                    Reportes = 1
                });
            }
        }

        UbicacionesCombinadas = ubicaciones;

        // Poblar campos del perfil extendido en el ViewModel (misma carga, sin query extra)
        if (perfil != null)
        {
            Medico!.Foto             = perfil.Foto;
            Medico!.Biografia        = perfil.Biografia;
            Medico!.HorariosAtencion = perfil.HorariosAtencion;
            Medico!.SitioWeb         = perfil.SitioWeb;
            Medico!.Instagram        = perfil.Instagram;
            Medico!.LinkedIn         = perfil.LinkedIn;
        }
    }

    private Guid? ObtenerUsuarioId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is not null ? Guid.Parse(value) : null;
    }
}
