using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Pages.DirectorioMedicos;

public class IndexModel : PageModel
{
    private readonly IMedicoDirectorioService _service;

    public IndexModel(IMedicoDirectorioService service)
        => _service = service;

    public DirectorioIndexVm Directorio { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Especialidad { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Directorio = await _service.GetListadoAsync(Busqueda, Estado, Especialidad, AreaId, Pagina);
    }
}
