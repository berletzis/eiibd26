namespace eiibd26.DTOs.Analytics;

public sealed class MoodStatsDto
{
    public int TotalRegistros { get; init; }
    /// <summary>Texto descriptivo del promedio: "Muy malo", "Malo", "Regular", "Bueno", "Muy bueno".</summary>
    public string Promedio { get; init; } = string.Empty;
}
