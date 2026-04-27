using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;

namespace eiibd26.Services.Analytics;

public interface IHealthInsightService
{
    /// <summary>
    /// Genera insights clínicos básicos a partir de síntomas ya consultados.
    /// No realiza consultas a base de datos.
    /// </summary>
    HealthInsightDto Analizar(List<SintomaExportDto> sintomas);
}
