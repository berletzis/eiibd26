namespace eiibd26.DTOs.Export;

public sealed class EstadoAnimoExportDto
{
    public DateTime FechaRegistro { get; init; }
    public int EstadoMood { get; init; }
    public string EstadoMoodNombre { get; init; } = string.Empty;
    public string? Texto { get; init; }
}
