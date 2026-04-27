namespace eiibd26.DTOs.Tracking;

/// <summary>
/// Datos necesarios para realizar un upsert de tracking de síntoma.
/// La fecha se usa únicamente para el lookup por día; el timestamp exacto lo asigna el servicio.
/// Las validaciones de ownership y parsing de entrada son responsabilidad del handler llamante.
/// </summary>
public record TrackingRequestDto(
    Guid     IdUsuario,
    int      IdSintomaUsuario,
    DateTime Fecha,
    string   Estado,
    int?     Dolor,
    int?     FrecuenciaId,
    bool?    TieneSangrado
);
