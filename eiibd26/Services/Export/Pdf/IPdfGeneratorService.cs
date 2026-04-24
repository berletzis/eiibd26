using eiibd26.DTOs.Export;

namespace eiibd26.Services.Export.Pdf;

public interface IPdfGeneratorService
{
    /// <summary>Genera el PDF del resumen médico y retorna los bytes del archivo.</summary>
    byte[] GenerarPdf(MedicalSummaryDto data);
}
