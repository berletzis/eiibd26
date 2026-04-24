using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;

namespace eiibd26.Services.Analytics;

public interface IHealthStatsService
{
    /// <summary>
    /// Calcula estadísticas de salud a partir de listas ya consultadas.
    /// No realiza consultas a base de datos.
    /// </summary>
    HealthStatsDto Calcular(
        List<EstadoAnimoExportDto> estados,
        List<SintomaExportDto> sintomas);
}
