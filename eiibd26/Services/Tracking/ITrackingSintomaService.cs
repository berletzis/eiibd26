using eiibd26.DTOs.Tracking;
using eiibd26.Models;

namespace eiibd26.Services.Tracking;

/// <summary>
/// Centraliza la lógica de upsert de tracking de síntoma.
/// No valida ownership ni accede a HttpContext.
/// </summary>
public interface ITrackingSintomaService
{
    /// <summary>
    /// Inserta o actualiza el tracking de un síntoma para el día indicado en <paramref name="request"/>.Fecha.
    /// Aplica reglas de consistencia según TipoSintoma: si no es medible (tipo != 1),
    /// FrecuenciaId y TieneSangrado se anulan automáticamente.
    /// En update, la propiedad Fecha del registro existente no se modifica.
    /// En insert, se asigna Fecha = request.Fecha.Date + DateTime.Now.TimeOfDay.
    /// </summary>
    Task<TrackingSintomaUsuario> GuardarTrackingAsync(TrackingRequestDto request, CancellationToken ct = default);
}
