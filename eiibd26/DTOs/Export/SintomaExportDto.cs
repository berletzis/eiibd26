namespace eiibd26.DTOs.Export;

public sealed class SintomaExportDto
{
    public string NombreSintoma { get; init; } = string.Empty;
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public List<string> Tratamientos { get; init; } = [];
    public List<TrackingSintomaExportDto> Trackings { get; init; } = [];
}
