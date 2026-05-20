using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Pages.DirectorioMedicos;

public class DetalleModel : PageModel
{
    private readonly IMedicoDirectorioService _service;

    public DetalleModel(IMedicoDirectorioService service)
        => _service = service;

    public MedicoDetalleVm? Medico { get; set; }
    public List<TipoConfirmacion> TiposConfirmacion { get; set; } = new();

    [BindProperty]
    public ConfirmarAtencionVm ConfirmarVm { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        Medico = await _service.GetDetalleAsync(id, usuarioId);
        if (Medico is null) return NotFound();

        TiposConfirmacion = await _service.GetTiposConfirmacionActivosAsync();
        ConfirmarVm.MedicoDirectorioId = id;
        return Page();
    }

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

        return RedirectToPage(new { id = ConfirmarVm.MedicoDirectorioId });
    }

    private Guid? ObtenerUsuarioId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return value is not null ? Guid.Parse(value) : null;
    }
}
