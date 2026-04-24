using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;

namespace eiibd26.Services.Analytics;

/// <summary>
/// Calcula estadísticas básicas de salud a partir de listas ya cargadas.
/// No depende de DbContext ni realiza consultas a base de datos.
/// </summary>
public sealed class HealthStatsService : IHealthStatsService
{
    // Mapeo de string Estado → valor numérico (según TrackingSintomaUsuario.Estado)
    private static readonly Dictionary<string, int> EstadoSintomaValor = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ninguno"]  = 0,
        ["Leve"]     = 1,
        ["Moderado"] = 2,
        ["Severo"]   = 3
    };

    public HealthStatsDto Calcular(
        List<EstadoAnimoExportDto> estados,
        List<SintomaExportDto> sintomas)
    {
        return new HealthStatsDto
        {
            Mood     = CalcularMood(estados),
            Symptoms = CalcularSintomas(sintomas)
        };
    }

    // ── MOOD ──────────────────────────────────────────────────────────────────

    private static MoodStatsDto CalcularMood(List<EstadoAnimoExportDto> estados)
    {
        if (estados.Count == 0)
            return new MoodStatsDto { TotalRegistros = 0, Promedio = "Sin registros" };

        var promedio = estados.Average(e => e.EstadoMood);

        return new MoodStatsDto
        {
            TotalRegistros = estados.Count,
            Promedio       = MapearPromedioMood(promedio)
        };
    }

    /// <summary>Mapea el promedio numérico (1–5) del enum EstadoAnimoEnum a texto.</summary>
    private static string MapearPromedioMood(double promedio) => promedio switch
    {
        <= 1.5 => "Muy malo",
        <= 2.5 => "Malo",
        <= 3.5 => "Regular",
        <= 4.5 => "Bueno",
        _      => "Muy bueno"
    };

    // ── SÍNTOMAS ──────────────────────────────────────────────────────────────

    private static SymptomStatsDto CalcularSintomas(List<SintomaExportDto> sintomas)
    {
        // Aplanar todos los trackings de todos los síntomas, ordenados por fecha
        var trackings = sintomas
            .SelectMany(s => s.Trackings)
            .OrderBy(t => t.Fecha)
            .ToList();

        var total = trackings.Count;

        if (total < 2)
            return new SymptomStatsDto { TotalRegistros = total, Tendencia = "Sin datos suficientes" };

        // Tomar primer y último valor numérico conocido
        var primero = ValorEstado(trackings.First().Estado);
        var ultimo  = ValorEstado(trackings.Last().Estado);

        // Si alguno es desconocido, no se puede calcular tendencia
        if (primero < 0 || ultimo < 0)
            return new SymptomStatsDto { TotalRegistros = total, Tendencia = "Sin datos suficientes" };

        // Todos los valores iguales → Estable sin ambigüedad
        var todosIguales = trackings.All(t => ValorEstado(t.Estado) == primero);
        if (todosIguales)
            return new SymptomStatsDto { TotalRegistros = total, Tendencia = "Estable" };

        var tendencia = ultimo.CompareTo(primero) switch
        {
            > 0 => "Empeorando",
            < 0 => "Mejorando",
            _   => "Estable"
        };

        return new SymptomStatsDto { TotalRegistros = total, Tendencia = tendencia };
    }

    /// <summary>Convierte el string Estado a valor numérico. Devuelve -1 si es desconocido.</summary>
    private static int ValorEstado(string estado) =>
        EstadoSintomaValor.TryGetValue(estado ?? string.Empty, out var val) ? val : -1;
}
