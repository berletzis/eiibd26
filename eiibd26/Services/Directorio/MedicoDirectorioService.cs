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

        // PERF-015: Obtener IDs de la página primero, luego resolver confirmaciones
        // en una sola query agregada — elimina 2 subqueries correlacionadas por médico.
        var medicoIds = await query
            .OrderByDescending(m => (int)m.NivelConfianza)
            .ThenBy(m => m.NombreCompleto)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(m => m.Id)
            .ToListAsync();

        // Query única de confirmaciones para todos los médicos de la página
        var confirmacionesPorMedico = await _db.ConfirmacionesComunitarias
            .AsNoTracking()
            .Where(c => medicoIds.Contains(c.MedicoDirectorioId) && !c.Eliminado)
            .GroupBy(c => c.MedicoDirectorioId)
            .Select(g => new
            {
                MedicoId            = g.Key,
                Total               = g.Count(),
                PacientesUnicos     = g.Select(c => c.UsuarioId).Distinct().Count()
            })
            .ToDictionaryAsync(x => x.MedicoId);

        var medicos = await query
            .OrderByDescending(m => (int)m.NivelConfianza)
            .ThenBy(m => m.NombreCompleto)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(m => new
            {
                m.Id,
                m.NombreCompleto,
                m.Especialidad,
                m.Subespecialidad,
                m.Estado,
                m.Ciudad,
                m.HospitalClinica,
                m.NivelConfianza,
                m.EstatusValidacion,
                AreasExperiencia = m.AreasExperiencia
                    .Select(ae => ae.AreaExperienciaEii.Nombre)
                    .ToList()
            })
            .ToListAsync();

        var medicoCards = medicos.Select(m =>
        {
            confirmacionesPorMedico.TryGetValue(m.Id, out var conf);
            return new MedicoCardVm
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
                TotalConfirmaciones  = conf?.Total ?? 0,
                TotalPacientesUnicos = conf?.PacientesUnicos ?? 0,
                AreasExperiencia     = m.AreasExperiencia
            };
        }).ToList();

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
            Medicos            = medicoCards,
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
                TotalConfirmaciones  = _db.ConfirmacionesComunitarias
                    .Count(c => c.MedicoDirectorioId == m.Id && !c.Eliminado),
                TotalPacientesUnicos = _db.ConfirmacionesComunitarias
                    .Where(c => c.MedicoDirectorioId == m.Id && !c.Eliminado)
                    .Select(c => c.UsuarioId).Distinct().Count(),
                AreasExperiencia     = m.AreasExperiencia
                    .Select(ae => new AreaExperienciaVm
                    {
                        Id     = ae.AreaExperienciaEiiId,
                        Nombre = ae.AreaExperienciaEii.Nombre
                    }).ToList(),
                // ConfirmacionesAgregadas: se mantiene desde ConfirmacionComunitaria
                // porque representa confirmaciones de tipo/rol comunitario (diferente semántica).
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
        var especialidadTrim = vm.Especialidad?.Trim();
        var yaExiste = await _db.MedicosDirectorio
            .AnyAsync(m =>
                m.NombreCompleto == vm.NombreCompleto.Trim() &&
                m.Especialidad == especialidadTrim &&
                !m.Eliminado);

        if (yaExiste)
            throw new InvalidOperationException(
                $"Ya existe un médico registrado con el nombre '{vm.NombreCompleto}' y especialidad '{vm.Especialidad}'.");

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
            VisiblePublicamente   = false,
            Activo                = false,
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

    /// <summary>
    /// Fuente canónica: ConfirmacionesComunitarias.
    /// Recalcula NivelConfianza considerando total de confirmaciones,
    /// cédula verificada y perfil reclamado.
    /// </summary>
    public async Task RecalcularNivelConfianzaAsync(int medicoId)
    {
        var medico = await _db.MedicosDirectorio.FindAsync(medicoId);
        if (medico is null) return;

        var total = await _db.ConfirmacionesComunitarias
            .CountAsync(c => c.MedicoDirectorioId == medicoId && !c.Eliminado);

        // The platform is EII-specific — any community confirmation implies EII context
        var tieneEII = total > 0;

        medico.NivelConfianza = (NivelConfianzaEnum)CalcularNivelVerificacion(
            total, tieneEII, medico.CedulaVerificada, medico.PerfilReclamado);
        medico.FechaModificacion = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static int CalcularNivelVerificacion(
        int totalConfirmaciones, bool tieneConfirmacionEII,
        bool cedulaVerificada, bool perfilReclamado)
    {
        if (perfilReclamado) return 3;
        if (cedulaVerificada || totalConfirmaciones >= 5) return 2;
        if (totalConfirmaciones >= 3 && tieneConfirmacionEII) return 1;
        return 0;
    }

    public async Task<List<TipoConfirmacion>> GetTiposConfirmacionActivosAsync()
        => await _db.TiposConfirmacion
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Orden)
            .ToListAsync();
}
