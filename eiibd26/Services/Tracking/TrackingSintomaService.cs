using eiibd26.DTOs.Tracking;
using eiibd26.Models;
using Microsoft.EntityFrameworkCore;

namespace eiibd26.Services.Tracking;

public sealed class TrackingSintomaService : ITrackingSintomaService
{
    private readonly ApplicationDbContext _db;

    public TrackingSintomaService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TrackingSintomaUsuario> GuardarTrackingAsync(TrackingRequestDto request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Regla de consistencia: solo síntomas medibles (tipo 1) admiten frecuencia y sangrado
        var tipoSintoma = await _db.sintomasUsuario
            .Where(x => x.id == request.IdSintomaUsuario && x.idUsuario == request.IdUsuario)
            .Select(x => x.Sintoma != null ? x.Sintoma.TipoSintoma : 0)
            .FirstOrDefaultAsync(ct);

        int?  frecuenciaId  = tipoSintoma == 1 ? request.FrecuenciaId  : null;
        bool? tieneSangrado = tipoSintoma == 1 ? request.TieneSangrado : null;
        // Dolor se acepta para todos los tipos (0=Subjetivo, 1=Medible, 2=Dolor)

        var fechaDia = request.Fecha.Date;

        var existing = await _db.TrackingSintomaUsuario
            .FirstOrDefaultAsync(
                t => t.IdSintomaUsuario == request.IdSintomaUsuario
                  && t.IdUsuario        == request.IdUsuario
                  && t.Fecha.Date       == fechaDia,
                ct);

        if (existing != null)
        {
            existing.Estado        = request.Estado;
            existing.Dolor         = request.Dolor;
            existing.TieneSangrado = tieneSangrado;
            existing.FrecuenciaId  = frecuenciaId;
            // Fecha no se modifica en update
            _db.TrackingSintomaUsuario.Update(existing);
            await _db.SaveChangesAsync(ct);
            return existing;
        }

        var nuevo = new TrackingSintomaUsuario
        {
            IdUsuario        = request.IdUsuario,
            IdSintomaUsuario = request.IdSintomaUsuario,
            Fecha            = fechaDia + DateTime.Now.TimeOfDay,
            Estado           = request.Estado,
            Dolor            = request.Dolor,
            TieneSangrado    = tieneSangrado,
            FrecuenciaId     = frecuenciaId
        };
        _db.TrackingSintomaUsuario.Add(nuevo);
        await _db.SaveChangesAsync(ct);
        return nuevo;
    }
}
