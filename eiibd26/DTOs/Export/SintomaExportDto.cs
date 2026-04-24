namespace eiibd26.DTOs.Export;

public sealed class SintomaExportDto
{
    public string NombreSintoma { get; init; } = string.Empty;
    public List<string> Tratamientos { get; init; } = [];
    public List<TrackingSintomaExportDto> Trackings { get; init; } = [];
}
