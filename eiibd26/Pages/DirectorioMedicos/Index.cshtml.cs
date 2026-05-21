using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;

namespace eiibd26.Pages.DirectorioMedicos;

public class IndexModel : PageModel
{
    private readonly IMedicoDirectorioService _service;
    private readonly ApplicationDbContext _db;

    public IndexModel(IMedicoDirectorioService service, ApplicationDbContext db)
    {
        _service = service;
        _db = db;
    }

    public DirectorioIndexVm Directorio { get; set; } = new();

    // Para los badges públicos: EII por medico
    public Dictionary<int, bool> MedicosConEII { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? Busqueda     { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado       { get; set; }
    [BindProperty(SupportsGet = true)] public string? Especialidad { get; set; }
    [BindProperty(SupportsGet = true)] public int?    AreaId       { get; set; }
    [BindProperty(SupportsGet = true)] public int     Pagina       { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Directorio = await _service.GetListadoAsync(Busqueda, Estado, Especialidad, AreaId, Pagina);

        if (Directorio.Medicos.Any())
        {
            var ids = Directorio.Medicos.Select(m => m.Id).ToList();
            var conEII = await _db.DirectorioMedicoConfirmaciones
                .AsNoTracking()
                .Where(c => ids.Contains(c.MedicoId) && !c.Eliminado &&
                            (c.TieneExperienciaEII || c.ExpCUCI || c.ExpCrohn ||
                             c.ExpPediatrico || c.ExpOstomias || c.ExpBiologicos ||
                             c.ExpEmbarazoEII || c.ExpManejoBrotes || c.ExpSegundaOpinion ||
                             c.ExpCirugia || c.ExpSeguimientoProlongado))
                .Select(c => c.MedicoId)
                .Distinct()
                .ToListAsync();
            MedicosConEII = conEII.ToDictionary(id => id, _ => true);
        }
    }
}
