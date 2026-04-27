namespace eiibd26.DTOs.Analytics;

/// <summary>
/// Tendencia calculada para un síntoma individual mediante regresión OLS.
/// </summary>
public record SymptomTrendDto(
    string NombreSintoma,
    string Tendencia,
    int    TotalRegistros
);
