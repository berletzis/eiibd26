using eiibd26.DTOs.Export;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Export;

public sealed class MedicalSummaryService : IMedicalSummaryService
{
    private const int Limit = 500;

    private readonly ApplicationDbContext _db;
    private readonly ILogger<MedicalSummaryService> _logger;

    public MedicalSummaryService(ApplicationDbContext db, ILogger<MedicalSummaryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MedicalSummaryDto> GenerarAsync(Guid userId, DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        if (desde > hasta)
            throw new ArgumentException("La fecha de inicio no puede ser mayor a la fecha de fin.");

        _logger.LogInformation("Generando resumen médico para usuario {UserId} desde {Desde:d} hasta {Hasta:d}.", userId, desde, hasta);

        var perfil       = await ObtenerPerfilAsync(userId, ct);
        var condiciones  = await ObtenerCondicionesAsync(userId, ct);
        var estados      = await ObtenerEstadosAnimoAsync(userId, desde, hasta, ct);
        var sintomas     = await ObtenerSintomasConTrackingAsync(userId, desde, hasta, ct);
        var tratamientos = await ObtenerTratamientosAsync(userId, desde, hasta, ct);
        var laboratorios = await ObtenerLaboratoriosAsync(userId, desde, hasta, ct);

        bool truncado = estados.Count >= Limit
                     || sintomas.Sum(s => s.Trackings.Count) >= Limit
                     || tratamientos.Count >= Limit;

        return new MedicalSummaryDto
        {
            Perfil           = perfil,
            Condiciones      = condiciones,
            EstadosAnimo     = estados,
            Sintomas         = sintomas,
            Tratamientos     = tratamientos,
            Laboratorios     = laboratorios,
            Desde            = desde,
            Hasta            = hasta,
            FechaGeneracion  = DateTime.Now,
            DatosTruncados   = truncado,
            LimiteAplicado   = Limit
        };
    }

    // ── Perfil ────────────────────────────────────────────────────────────────

    private async Task<PerfilExportDto> ObtenerPerfilAsync(Guid userId, CancellationToken ct)
    {
        var perfil = await _db.Perfil
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.idUser == userId, ct);

        if (perfil is null) return new PerfilExportDto();

        return new PerfilExportDto
        {
            NombreCompleto    = $"{perfil.Nombre} {perfil.Apellidos}".Trim(),
            Genero            = perfil.Genero,
            FechaDeNacimiento = perfil.FechaDeNacimiento,
            NombreCiudad      = perfil.NombreCiudad,
            NombrePais        = perfil.NombrePais,
            AcercaDe          = perfil.AcercaDe
        };
    }

    // ── Condiciones ───────────────────────────────────────────────────────────

    private async Task<List<CondicionExportDto>> ObtenerCondicionesAsync(Guid userId, CancellationToken ct)
    {
        return await _db.condicionUsuario
            .AsNoTracking()
            .Where(c => c.idUsuario == userId && !c.Eliminado)
            .Include(c => c.Condicion)
            .OrderBy(c => c.fechaInicio)
            .Select(c => new CondicionExportDto
            {
                Nombre      = c.Condicion.nombre,
                FechaInicio = c.fechaInicio
            })
            .ToListAsync(ct);
    }

    // ── Estado de ánimo ───────────────────────────────────────────────────────

    private async Task<List<EstadoAnimoExportDto>> ObtenerEstadosAnimoAsync(
        Guid userId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        return await _db.EstadoAnimoUsuario
            .AsNoTracking()
            .Where(x => x.IdUsuario == userId && !x.Eliminado)
            .Where(x => x.FechaRegistro >= desde && x.FechaRegistro <= hasta)
            .OrderBy(x => x.FechaRegistro)
            .Take(Limit)
            .Select(x => new EstadoAnimoExportDto
            {
                FechaRegistro    = x.FechaRegistro,
                EstadoMood       = (int)x.EstadoMood,
                EstadoMoodNombre = x.EstadoMood.ToString(),
                Texto            = x.Texto
            })
            .ToListAsync(ct);
    }

    // ── Síntomas con tracking ─────────────────────────────────────────────────

    private async Task<List<SintomaExportDto>> ObtenerSintomasConTrackingAsync(
        Guid userId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        // Obtener los trackings del período
        var trackings = await _db.TrackingSintomaUsuario
            .AsNoTracking()
            .Where(t => t.IdUsuario == userId)
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta)
            .Include(t => t.SintomaUsuario)
                .ThenInclude(su => su.Sintoma)
            .Include(t => t.Frecuencia)
            .OrderBy(t => t.Fecha)
            .Take(Limit)
            .ToListAsync(ct);

        // Obtener IDs de sintomasUsuario involucrados
        var idsSintomasUsuario = trackings
            .Where(t => t.SintomaUsuario != null)
            .Select(t => t.IdSintomaUsuario)
            .Distinct()
            .ToList();

        // Cargar tratamientos asociados a esos síntomas
        var tratamientosPorSintoma = await _db.TratamientoSintomaUsuario
            .AsNoTracking()
            .Where(ts => ts.IdSintomaUsuario != null && idsSintomasUsuario.Contains(ts.IdSintomaUsuario.Value))
            .Include(ts => ts.TratamientoUsuario)
                .ThenInclude(tu => tu.Tratamiento)
            .ToListAsync(ct);

        var mapaTratamientos = tratamientosPorSintoma
            .Where(ts => ts.TratamientoUsuario?.Tratamiento != null)
            .GroupBy(ts => ts.IdSintomaUsuario!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ts => ts.TratamientoUsuario!.Tratamiento!.nombre)
                       .Distinct()
                       .OrderBy(n => n)
                       .ToList());

        // Agrupar por síntoma
        return trackings
            .Where(t => t.SintomaUsuario?.Sintoma != null)
            .GroupBy(t => new { t.IdSintomaUsuario, NombreSintoma = t.SintomaUsuario!.Sintoma!.nombre })
            .Select(g =>
            {
                // SintomaUsuario ya viene Included arriba: no hay query nueva.
                // fechaInicio puede traer el sentinel 1753 -> en ese caso no se reporta fecha de inicio.
                var su = g.First().SintomaUsuario!;
                return new SintomaExportDto
                {
                    NombreSintoma = g.Key.NombreSintoma,
                    FechaInicio   = su.fechaInicio >= new DateTime(1753, 1, 1) ? su.fechaInicio : null,
                    FechaFin      = su.FechaFin,
                    Tratamientos  = mapaTratamientos.TryGetValue(g.Key.IdSintomaUsuario, out var trts) ? trts : [],
                    Trackings     = g.Select(t => new TrackingSintomaExportDto
                    {
                        Fecha           = t.Fecha,
                        Estado          = t.Estado,
                        Dolor           = t.Dolor,
                        TieneSangrado   = t.TieneSangrado,
                        FrecuenciaNombre = t.Frecuencia?.Nombre
                    }).OrderBy(x => x.Fecha).ToList()
                };
            })
            .OrderBy(s => s.NombreSintoma)
            .ToList();
    }

    // ── Tratamientos ──────────────────────────────────────────────────────────

    private async Task<List<LaboratoryExportDto>> ObtenerLaboratoriosAsync(
        Guid userId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        return await _db.PatientLaboratoryResults
            .AsNoTracking()
            .Where(r => r.PatientId == userId && !r.Eliminado)
            .Where(r => !r.ResultDate.HasValue || (r.ResultDate >= desde && r.ResultDate <= hasta))
            .Include(r => r.LaboratoryType).ThenInclude(t => t!.Parent)
            .Include(r => r.LaboratoryUnit)
            .Include(r => r.CondicionUsuario).ThenInclude(cu => cu!.Condicion)
            .OrderByDescending(r => r.ResultDate ?? (DateTime?)r.FechaCreado)
            .Take(Limit)
            .Select(r => new LaboratoryExportDto
            {
                Estudio      = r.LaboratoryType!.Parent!.Name ?? r.LaboratoryType.Name,
                Categoria    = r.LaboratoryType.Name,
                Resultado    = r.ResultValue,
                Unidad       = r.LaboratoryUnit != null ? r.LaboratoryUnit.Abreviatura : r.ResultUnit,
                FechaResultado = r.ResultDate,
                Notas        = r.Notes,
                Condicion    = r.CondicionUsuario != null ? r.CondicionUsuario.Condicion.nombre : null
            })
            .ToListAsync(ct);
    }

    private async Task<List<TratamientoExportDto>> ObtenerTratamientosAsync(
        Guid userId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        return await _db.tratamientoUsuario
            .AsNoTracking()
            .Where(t => t.idUsuario == userId && !t.Eliminado)
            .Where(t => t.fechaInicio <= hasta)
            .Include(t => t.Tratamiento)
            .OrderBy(t => t.fechaInicio)
            .Take(Limit)
            .Select(t => new TratamientoExportDto
            {
                Nombre      = t.Tratamiento.nombre,
                FechaInicio = t.fechaInicio,
                FechaFin    = t.FechaFin
            })
            .ToListAsync(ct);
    }
}
