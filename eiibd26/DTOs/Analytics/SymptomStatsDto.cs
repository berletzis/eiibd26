namespace eiibd26.DTOs.Analytics;

public sealed class SymptomStatsDto
{
    public int TotalRegistros { get; init; }
    /// <summary>Tendencia global (peor caso): "Mejorando", "Estable", "Empeorando" o "Sin datos suficientes".</summary>
    public string Tendencia { get; init; } = string.Empty;
    /// <summary>Tendencia calculada individualmente por síntoma mediante OLS.</summary>
    public List<SymptomTrendDto> PorSintoma { get; init; } = [];
}
