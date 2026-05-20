using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Pages.DirectorioMedicos;

[Authorize]
public class ProponerModel : PageModel
{
    private readonly IMedicoDirectorioService _service;

    public ProponerModel(IMedicoDirectorioService service)
        => _service = service;

    [BindProperty]
    public ProponerMedicoVm Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var datos = await _service.GetProponerVmAsync();
        Input.AreasDisponibles = datos.AreasDisponibles;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var datos = await _service.GetProponerVmAsync();
            Input.AreasDisponibles = datos.AreasDisponibles;
            return Page();
        }

        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var usuarioId = Guid.Parse(value);
        var medicoId = await _service.ProponerMedicoAsync(Input, usuarioId);
        TempData["Success"] = "Gracias por tu aporte. El médico fue registrado en el directorio comunitario.";
        return RedirectToPage("Detalle", new { id = medicoId });
    }
}
