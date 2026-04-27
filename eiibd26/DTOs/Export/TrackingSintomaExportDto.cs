namespace eiibd26.DTOs.Export;

public sealed class TrackingSintomaExportDto
{
    public DateTime Fecha { get; init; }
    public string Estado { get; init; } = string.Empty;

    /// <summary>Nivel de dolor (0–10). Null si no fue registrado.</summary>
    public int? Dolor { get; init; }

    /// <summary>Indica si hubo sangrado en el registro. Null si no fue informado.</summary>
    public bool? TieneSangrado { get; init; }

    /// <summary>Texto de frecuencia del catálogo. Null si no fue registrado.</summary>
    public string? FrecuenciaNombre { get; init; }
}
