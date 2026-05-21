using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Pages.DirectorioMedicos;

[Authorize]
public class ReclamarPerfilModel : PageModel
{
    private readonly ApplicationDbContext _db;
    public ReclamarPerfilModel(ApplicationDbContext db) => _db = db;

    public MedicoDirectorio? Medico { get; set; }

    [BindProperty] public string? EmailContacto { get; set; }
    [BindProperty] public string? CedulaProfesionalDeclarada { get; set; }
    [BindProperty] public bool Confirmo { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Medico = await _db.MedicosDirectorio.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && !m.Eliminado);

        if (Medico is null) return NotFound();

        if (Medico.PerfilReclamado)
        {
            TempData["Error"] = "Este perfil ya fue reclamado por el médico titular.";
            return RedirectToPage("Detalle", new { id });
        }

        if (Medico.SolicitudClaimPendiente)
        {
            TempData["Error"] = "Ya existe una solicitud pendiente de revisión para este perfil.";
            return RedirectToPage("Detalle", new { id });
        }

        // Pre-llenar con el email del usuario autenticado
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            EmailContacto = user?.Email ?? string.Empty;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Medico = await _db.MedicosDirectorio.FirstOrDefaultAsync(m => m.Id == id && !m.Eliminado);
        if (Medico is null) return NotFound();

        if (Medico.PerfilReclamado || Medico.SolicitudClaimPendiente)
            return RedirectToPage("Detalle", new { id });

        if (!Confirmo)
        {
            ModelState.AddModelError(nameof(Confirmo), "Debes confirmar que eres el médico indicado.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CedulaProfesionalDeclarada))
        {
            ModelState.AddModelError(nameof(CedulaProfesionalDeclarada), "La cédula profesional es obligatoria.");
            return Page();
        }

        Medico.EstatusReclamacion = EstatusReclamacion.EnProceso;
        Medico.EmailSolicitudClaim = EmailContacto?.Trim();
        Medico.CedulaProfesional = CedulaProfesionalDeclarada.Trim();
        Medico.FechaSolicitudClaim = DateTime.UtcNow;
        Medico.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Tu solicitud fue enviada. El equipo de EIIBD te contactará al email indicado.";
        return RedirectToPage("Detalle", new { id });
    }
}
