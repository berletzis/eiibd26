using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using eiibd26.Models.Directorio;
using eiibd26.Models.Medico;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Pages.Directorio;

public class ReclamarModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReclamarModel> _logger;

    public ReclamarModel(ApplicationDbContext db, IEmailSender emailSender, ILogger<ReclamarModel> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public int MedicoId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "El correo es requerido.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    public MedicoDirectorio? Medico { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MedicoId && m.Activo && !m.Eliminado);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == MedicoId && m.Activo && !m.Eliminado);

        if (Medico is null)
        {
            TempData["Error"] = "Médico no encontrado.";
            return Page();
        }

        if (!ModelState.IsValid) return Page();

        var yaVinculado = await _db.MedicosPerfilExtendido
            .AnyAsync(p => p.MedicoId == MedicoId && p.UserId != null);
        if (yaVinculado)
        {
            TempData["Error"] = "Este perfil ya fue reclamado por un médico verificado.";
            return Page();
        }

        // Invalidar tokens activos previos
        var tokensActivos = await _db.MedicosReclamacionToken
            .Where(t => t.MedicoId == MedicoId && t.Activo && t.FechaUsado == null)
            .ToListAsync();
        foreach (var t in tokensActivos) t.Activo = false;

        var token = Guid.NewGuid().ToString("N");
        _db.MedicosReclamacionToken.Add(new MedicoReclamacionToken
        {
            MedicoId     = MedicoId,
            Token        = token,
            EmailDestino = Email.Trim().ToLowerInvariant(),
            FechaCreado  = DateTime.UtcNow,
            FechaExpira  = DateTime.UtcNow.AddHours(72),
            Activo       = true
        });
        await _db.SaveChangesAsync();

        try
        {
            var link = $"{Request.Scheme}://{Request.Host}/directorio/activar?token={token}";
            await _emailSender.SendEmailAsync(Email,
                "Enlace para reclamar tu perfil — EIIBD",
                $"<p>Hola,</p>" +
                $"<p>Recibimos tu solicitud para reclamar el perfil de <strong>{HtmlEncoder.Default.Encode(Medico.NombreCompleto)}</strong> en el directorio EII.</p>" +
                $"<p><a href='{link}'>Haz clic aquí para completar la verificación</a></p>" +
                $"<p>Este enlace expira en <strong>72 horas</strong>.</p>" +
                $"<p>Si no solicitaste esto, ignora este mensaje.</p>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando email de reclamación a {Email}", Email);
        }

        TempData["Success"] = $"Te enviamos un correo a {Email}. El link expira en 72 horas.";
        return RedirectToPage(new { medicoId = MedicoId });
    }
}
