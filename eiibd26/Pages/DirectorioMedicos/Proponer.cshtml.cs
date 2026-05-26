using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using eiibd26.Data;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Pages.DirectorioMedicos;

[Authorize]
public class ProponerModel : PageModel
{
    private readonly IMedicoDirectorioService _service;
    private readonly ApplicationDbContext _db;
    private readonly string _googleMapsApiKey;

    public string GoogleMapsApiKey => _googleMapsApiKey;
    public SelectList PaisesSelectList { get; set; } = new SelectList(Enumerable.Empty<object>());

    public ProponerModel(
        IMedicoDirectorioService service,
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _service = service;
        _db = db;
        _googleMapsApiKey = configuration["GoogleMaps:ApiKey"] ?? string.Empty;
    }

    [BindProperty]
    public ProponerMedicoVm Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await PopulatePaisesAsync();
        var datos = await _service.GetProponerVmAsync();
        Input.AreasDisponibles = datos.AreasDisponibles;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await PopulatePaisesAsync();
            var datos = await _service.GetProponerVmAsync();
            Input.AreasDisponibles = datos.AreasDisponibles;
            return Page();
        }

        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var usuarioId = Guid.Parse(value);

        int medicoId;
        try
        {
            medicoId = await _service.ProponerMedicoAsync(Input, usuarioId);
        }
        catch (InvalidOperationException ex)
        {
            await PopulatePaisesAsync();
            var datos = await _service.GetProponerVmAsync();
            Input.AreasDisponibles = datos.AreasDisponibles;
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        TempData["Success"] = "Gracias por tu aporte. El médico fue registrado en el directorio comunitario. Estará visible tras la revisión del equipo de EIIBD.";
        return RedirectToPage("Index");
    }

    private async Task PopulatePaisesAsync()
    {
        try
        {
            var paises = await _db.Paises
                .Where(p => !p.Borrado && p.VIsibleBuscador)
                .OrderBy(p => p.PaisNombre)
                .ToListAsync();
            PaisesSelectList = new SelectList(paises, "PaisCodigo", "PaisNombre", Input.NombrePais);
        }
        catch
        {
            PaisesSelectList = new SelectList(Enumerable.Empty<object>());
        }
    }
}
