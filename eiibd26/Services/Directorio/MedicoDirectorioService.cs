using Microsoft.EntityFrameworkCore;
using eiibd26.Data;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Services.Directorio;

public class MedicoDirectorioService : IMedicoDirectorioService
{
    private readonly ApplicationDbContext _db;

    public MedicoDirectorioService(ApplicationDbContext db)
        => _db = db;

    public async Task<DirectorioIndexVm> GetListadoAsync(
        string? busqueda,
        string? estado,
        string? especialidad,
        int? areaId,
        int pagina = 1,
        int porPagina = 18)
    {
        var query = _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.VisiblePublicamente && m.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(m =>
                m.NombreCompleto.Contains(busqueda) ||
                (m.Especialidad != null && m.Especialidad.Contains(busqueda)) ||
                (m.HospitalClinica != null && m.HospitalClinica.Contains(busqueda)));

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(m => m.Estado == estado);

        if (!string.IsNullOrWhiteSpace(especialidad))
            query = query.Where(m => m.Especialidad == especialidad);

        if (areaId.HasValue)
            query = query.Where(m =>
                m.AreasExperiencia.Any(ae => ae.AreaExperienciaEiiId == areaId.Value));

        var total = await query.CountAsync();

        var medicos = await query
            .OrderByDescending(m => (int)m.NivelConfianza)
            .ThenBy(m => m.NombreCompleto)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(m => new MedicoCardVm
            {
                Id                   = m.Id,
                NombreCompleto       = m.NombreCompleto,
                Especialidad         = m.Especialidad,
                Subespecialidad      = m.Subespecialidad,
                Estado               = m.Estado,
                Ciudad               = m.Ciudad,
                HospitalClinica      = m.HospitalClinica,
                NivelConfianza       = m.NivelConfianza,
                EstatusValidacion    = m.EstatusValidacion,
                TotalConfirmaciones  = m.Confirmaciones.Count(),
                TotalPacientesUnicos = m.Confirmaciones.Select(c => c.UsuarioId).Distinct().Count(),
                AreasExperiencia     = m.AreasExperiencia
                    .Select(ae => ae.AreaExperienciaEii.Nombre)
                    .ToList()
            })
            .ToListAsync();

        var estados = await _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.VisiblePublicamente && m.Activo && m.Estado != null)
            .Select(m => m.Estado!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        var areas = await _db.AreasExperienciaEii
            .AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();

        return new DirectorioIndexVm
        {
            Medicos            = medicos,
            FiltroBusqueda     = busqueda,
            FiltroEstado       = estado,
            FiltroEspecialidad = especialidad,
            FiltroAreaId       = areaId,
            AreasDisponibles   = areas,
            EstadosDisponibles = estados,
            TotalResultados    = total,
            PaginaActual       = pagina,
            TotalPaginas       = (int)Math.Ceiling((double)total / porPagina)
        };
    }

    public async Task<MedicoDetalleVm?> GetDetalleAsync(int medicoId, Guid? usuarioId)
    {
        var medico = await _db.MedicosDirectorio
            .AsNoTracking()
            .Where(m => m.Id == medicoId && m.VisiblePublicamente && m.Activo)
            .Select(m => new MedicoDetalleVm
            {
                Id                   = m.Id,
                NombreCompleto       = m.NombreCompleto,
                CedulaProfesional    = m.CedulaProfesional,
                Especialidad         = m.Especialidad,
                Subespecialidad      = m.Subespecialidad,
                Estado               = m.Estado,
                Ciudad               = m.Ciudad,
                MunicipioAlcaldia    = m.MunicipioAlcaldia,
                HospitalClinica      = m.HospitalClinica,
                Latitud              = m.Latitud,
                Longitud             = m.Longitud,
                NivelConfianza       = m.NivelConfianza,
                EstatusValidacion    = m.EstatusValidacion,
                EstatusReclamacion   = m.EstatusReclamacion,
                TotalConfirmaciones  = m.Confirmaciones.Count(),
                TotalPacientesUnicos = m.Confirmaciones.Select(c => c.UsuarioId).Distinct().Count(),
                AreasExperiencia     = m.AreasExperiencia
                    .Select(ae => new AreaExperienciaVm
                    {
                        Id     = ae.AreaExperienciaEiiId,
                        Nombre = ae.AreaExperienciaEii.Nombre
                    }).ToList(),
                ConfirmacionesAgregadas = m.Confirmaciones
                    .GroupBy(c => c.TipoConfirmacionId)
                    .Select(g => new ConfirmacionAgregadaVm
                    {
                        TipoConfirmacionId = g.Key,
                        NombreTipo         = g.First().TipoConfirmacion.Nombre,
                        Total              = g.Count()
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (medico is null) return null;

        if (usuarioId.HasValue)
        {
            var tiposConfirmados = await _db.ConfirmacionesComunitarias
                .AsNoTracking()
                .Where(c => c.MedicoDirectorioId == medicoId && c.UsuarioId == usuarioId.Value)
                .Select(c => c.TipoConfirmacionId)
                .ToListAsync();

            medico.UsuarioYaConfirmo = tiposConfirmados.Any();
            medico.TiposConfirmadosPorUsuario = tiposConfirmados;
        }

        return medico;
    }

    public async Task<ProponerMedicoVm> GetProponerVmAsync()
    {
        var areas = await _db.AreasExperienciaEii
            .AsNoTracking()
            .Where(a => a.Activo)
            .OrderBy(a => a.Orden)
            .ToListAsync();

        return new ProponerMedicoVm { AreasDisponibles = areas };
    }

    public async Task<int> ProponerMedicoAsync(ProponerMedicoVm vm, Guid usuarioId)
    {
        var medico = new MedicoDirectorio
        {
            NombreCompleto        = vm.NombreCompleto.Trim(),
            CedulaProfesional     = vm.CedulaProfesional?.Trim(),
            Especialidad          = vm.Especialidad?.Trim(),
            Subespecialidad       = vm.Subespecialidad?.Trim(),
            NombrePais            = vm.NombrePais?.Trim(),
            Estado                = vm.Estado.Trim(),
            Ciudad                = vm.Ciudad?.Trim(),
            MunicipioAlcaldia     = vm.MunicipioAlcaldia?.Trim(),
            HospitalClinica       = vm.HospitalClinica?.Trim(),
            Latitud               = vm.Latitud,
            Longitud              = vm.Longitud,
            EstatusValidacion     = EstatusValidacionCedula.PendienteValidacion,
            NivelConfianza        = NivelConfianzaEnum.Identificado,
            EstatusReclamacion    = EstatusReclamacion.NoReclamado,
            VisiblePublicamente   = true,
            Activo                = true,
            PropuestoPorUsuarioId = usuarioId,
            FechaCreacion         = DateTimeOffset.UtcNow
        };

        _db.MedicosDirectorio.Add(medico);
        await _db.SaveChangesAsync();

        if (vm.AreasSeleccionadas.Any())
        {
            var areas = vm.AreasSeleccionadas
                .Select(areaId => new MedicoExperienciaEii
                {
                    MedicoDirectorioId   = medico.Id,
                    AreaExperienciaEiiId = areaId,
                    FechaCreacion        = DateTimeOffset.UtcNow
                });
            _db.MedicoExperienciaEii.AddRange(areas);
            await _db.SaveChangesAsync();
        }

        return medico.Id;
    }

    public async Task<bool> ConfirmarAtencionAsync(int medicoId, int tipoConfirmacionId, Guid usuarioId)
    {
        var yaExiste = await _db.ConfirmacionesComunitarias
            .AnyAsync(c =>
                c.MedicoDirectorioId == medicoId &&
                c.UsuarioId == usuarioId &&
                c.TipoConfirmacionId == tipoConfirmacionId);

        if (yaExiste) return false;

        _db.ConfirmacionesComunitarias.Add(new ConfirmacionComunitaria
        {
            MedicoDirectorioId = medicoId,
            UsuarioId          = usuarioId,
            TipoConfirmacionId = tipoConfirmacionId,
            FechaCreacion      = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        await RecalcularNivelConfianzaAsync(medicoId);
        return true;
    }

    public async Task RecalcularNivelConfianzaAsync(int medicoId)
    {
        var medico = await _db.MedicosDirectorio.FindAsync(medicoId);
        if (medico is null) return;

        var pacientesUnicos = await _db.ConfirmacionesComunitarias
            .Where(c => c.MedicoDirectorioId == medicoId)
            .Select(c => c.UsuarioId)
            .Distinct()
            .CountAsync();

        medico.NivelConfianza = pacientesUnicos switch
        {
            >= 10 => NivelConfianzaEnum.Establecido,
            >= 5  => NivelConfianzaEnum.Reconocido,
            >= 3  => NivelConfianzaEnum.Confirmado,
            _     => NivelConfianzaEnum.Identificado
        };

        medico.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<TipoConfirmacion>> GetTiposConfirmacionActivosAsync()
        => await _db.TiposConfirmacion
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Orden)
            .ToListAsync();
}
