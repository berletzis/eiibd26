using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Services.Directorio;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;
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

    // Badges ganados por médico (código del badge)
    public Dictionary<int, HashSet<string>> BadgesPorMedico { get; set; } = new();

    // Propuestas del usuario autenticado con estado dinámico
    public List<PropuestaPacienteVm> MisPropuestas { get; set; } = new();

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
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.PropuestoPorUsuarioId == userId && !m.Eliminado)
                .OrderByDescending(m => m.FechaCreacion)
                .Select(m => new
                {
                    m.Id, m.NombreCompleto, m.Especialidad, m.Estado, m.FechaCreacion,
                    m.Activo, m.VisiblePublicamente, m.EstatusReclamacion, m.EstatusValidacion
                })
                .ToListAsync();

            MisPropuestas = rows.Select(m =>
            {
                // D-07: misma fórmula que MedicoDirectorio.EstadoDerivado para evitar divergencia
                var estado = m.Activo && m.VisiblePublicamente && m.EstatusReclamacion == EstatusReclamacion.Reclamado
                    ? EstadoProfesionalDerivado.Reclamado
                    : m.Activo && m.VisiblePublicamente
                      ? EstadoProfesionalDerivado.Publicado
                    : m.EstatusValidacion == EstatusValidacionCedula.Validado
                      ? EstadoProfesionalDerivado.CedulaVerificada
                    : EstadoProfesionalDerivado.Propuesto;
                return new PropuestaPacienteVm
                {
                    Id             = m.Id,
                    Nombre         = m.NombreCompleto,
                    Especialidad   = m.Especialidad,
                    Estado         = m.Estado,
                    Fecha          = m.FechaCreacion,
                    EstadoDerivado = estado
                };
            }).ToList();
        }

        if (Directorio.Medicos.Any())
        {
            var ids = Directorio.Medicos.Select(m => m.Id).ToList();

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
