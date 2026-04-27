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
        ["Severo"]   = 3,
        ["Extremo"]  = 4
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
        if (sintomas.Count == 0)
            return new SymptomStatsDto { TotalRegistros = 0, Tendencia = "Sin datos suficientes", PorSintoma = [] };

        var totalRegistros = sintomas.Sum(s => s.Trackings.Count);

        // Calcular tendencia individual por síntoma (sin mezclar series)
        var porSintoma = sintomas
            .Select(s => new SymptomTrendDto(
                NombreSintoma: s.NombreSintoma,
                Tendencia:     CalcularTendenciaIndividual(s.Trackings),
                TotalRegistros: s.Trackings.Count))
            .OrderBy(t => t.NombreSintoma)
            .ToList();

        // Tendencia global: peor caso
        string tendenciaGlobal;
        if (porSintoma.Any(t => t.Tendencia == "Empeorando"))
            tendenciaGlobal = "Empeorando";
        else if (porSintoma.Any(t => t.Tendencia == "Mejorando"))
            tendenciaGlobal = "Mejorando";
        else if (porSintoma.All(t => t.Tendencia == "Sin datos suficientes"))
            tendenciaGlobal = "Sin datos suficientes";
        else
            tendenciaGlobal = "Estable";

        return new SymptomStatsDto
        {
            TotalRegistros = totalRegistros,
            Tendencia      = tendenciaGlobal,
            PorSintoma     = porSintoma
        };
    }

    /// <summary>
    /// Aplica regresión OLS sobre los trackings de un único síntoma.
    /// Retorna "Sin datos suficientes" si hay menos de 2 puntos reconocidos.
    /// </summary>
    private static string CalcularTendenciaIndividual(List<TrackingSintomaExportDto> trackings)
    {
        var valores = trackings
            .OrderBy(t => t.Fecha)
            .Select(t => ValorEstado(t.Estado))
            .Where(v => v >= 0)
            .ToList();

        if (valores.Count < 2)
            return "Sin datos suficientes";

        int n       = valores.Count;
        double sumX  = n * (n - 1) / 2.0;
        double sumX2 = (n - 1) * (double)n * (2 * n - 1) / 6.0;
        double sumY  = valores.Sum();
        double sumXY = valores.Select((y, i) => i * (double)y).Sum();

        double denominador = n * sumX2 - sumX * sumX;
        double pendiente   = (n * sumXY - sumX * sumY) / denominador;

        const double Umbral = 0.05;
        return pendiente switch
        {
            >  Umbral => "Empeorando",
            < -Umbral => "Mejorando",
            _         => "Estable"
        };
    }

    /// <summary>Convierte el string Estado a valor numérico. Devuelve -1 si es desconocido.</summary>
    private static int ValorEstado(string estado) =>
        EstadoSintomaValor.TryGetValue(estado ?? string.Empty, out var val) ? val : -1;
}
