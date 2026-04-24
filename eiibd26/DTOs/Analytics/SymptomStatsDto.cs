namespace eiibd26.DTOs.Analytics;

public sealed class SymptomStatsDto
{
    public int TotalRegistros { get; init; }
    /// <summary>Tendencia calculada: "Mejorando", "Estable", "Empeorando" o "Sin datos suficientes".</summary>
    public string Tendencia { get; init; } = string.Empty;
}
