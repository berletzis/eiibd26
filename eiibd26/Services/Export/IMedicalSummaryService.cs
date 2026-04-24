using eiibd26.DTOs.Export;

namespace eiibd26.Services.Export;

public interface IMedicalSummaryService
{
    /// <summary>
    /// Genera el DTO unificado con todos los datos médicos del usuario en el rango indicado.
    /// </summary>
    Task<MedicalSummaryDto> GenerarAsync(Guid userId, DateTime desde, DateTime hasta, CancellationToken ct = default);
}
