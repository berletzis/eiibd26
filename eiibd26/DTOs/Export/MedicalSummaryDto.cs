namespace eiibd26.DTOs.Export;

/// <summary>Modelo unificado para el Resumen Médico. Independiente de entidades EF.</summary>
public sealed class MedicalSummaryDto
{
    public PerfilExportDto Perfil { get; init; } = new();
    public List<CondicionExportDto> Condiciones { get; init; } = [];
    public List<EstadoAnimoExportDto> EstadosAnimo { get; init; } = [];
    public List<SintomaExportDto> Sintomas { get; init; } = [];
    public List<TratamientoExportDto> Tratamientos { get; init; } = [];

    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public DateTime FechaGeneracion { get; init; } = DateTime.Now;

    /// <summary>True si alguna colección fue truncada al límite configurado.</summary>
    public bool DatosTruncados { get; init; }
    public int LimiteAplicado { get; init; }
}
