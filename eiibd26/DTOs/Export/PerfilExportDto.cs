namespace eiibd26.DTOs.Export;

public sealed class PerfilExportDto
{
    public string NombreCompleto { get; init; } = string.Empty;
    public string? Genero { get; init; }
    public DateTime? FechaDeNacimiento { get; init; }
    public string? NombreCiudad { get; init; }
    public string? NombrePais { get; init; }
    public string? AcercaDe { get; init; }
}
