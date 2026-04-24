namespace eiibd26.DTOs.Analytics;

public sealed class HealthStatsDto
{
    public MoodStatsDto Mood { get; init; } = new();
    public SymptomStatsDto Symptoms { get; init; } = new();
}
