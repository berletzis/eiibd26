using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;
using System.Security.Claims;

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

    // Badges ganados por médico (código del badge)
    public Dictionary<int, HashSet<string>> BadgesPorMedico { get; set; } = new();

    // Propuestas pendientes del usuario autenticado
    public List<(int Id, string Nombre, string? Especialidad, string? Estado, DateTimeOffset Fecha)> MisPropuestas { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? Busqueda     { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado       { get; set; }
    [BindProperty(SupportsGet = true)] public string? Especialidad { get; set; }
    [BindProperty(SupportsGet = true)] public int?    AreaId       { get; set; }
    [BindProperty(SupportsGet = true)] public int     Pagina       { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Directorio = await _service.GetListadoAsync(Busqueda, Estado, Especialidad, AreaId, Pagina);

        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(rawId, out var userId))
        {
            var rows = await _db.MedicosDirectorio
                .AsNoTracking()
                .Where(m => m.PropuestoPorUsuarioId == userId && !m.Activo)
                .OrderByDescending(m => m.FechaCreacion)
                .Select(m => new { m.Id, m.NombreCompleto, m.Especialidad, m.Estado, m.FechaCreacion })
                .ToListAsync();
            MisPropuestas = rows
                .Select(m => (m.Id, m.NombreCompleto, m.Especialidad, m.Estado, m.FechaCreacion))
                .ToList();
        }

        if (Directorio.Medicos.Any())
        {
            var ids = Directorio.Medicos.Select(m => m.Id).ToList();
            var conEII = await _db.ConfirmacionesComunitarias
                .AsNoTracking()
                .Where(c => ids.Contains(c.MedicoDirectorioId) && !c.Eliminado)
                .Select(c => c.MedicoDirectorioId)
                .Distinct()
                .ToListAsync();
            MedicosConEII = conEII.ToDictionary(id => id, _ => true);

            // Badges ganados en batch (evita N+1)
            var badgesRows = await _db.MedicosPerfilBadge
                .AsNoTracking()
                .Where(pb => ids.Contains(pb.MedicoId))
                .Join(_db.MedicosBadge, pb => pb.BadgeId, b => b.Id,
                      (pb, b) => new { pb.MedicoId, b.Codigo })
                .ToListAsync();

            BadgesPorMedico = badgesRows
                .GroupBy(x => x.MedicoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Codigo).ToHashSet());

            foreach (var medico in Directorio.Medicos)
                if (BadgesPorMedico.TryGetValue(medico.Id, out var badgeSet))
                    medico.BadgesGanados = badgeSet;
        }
    }
}
