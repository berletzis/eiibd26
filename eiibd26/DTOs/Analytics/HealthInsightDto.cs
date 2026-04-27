namespace eiibd26.DTOs.Analytics;

/// <summary>Resultado del análisis clínico básico del período.</summary>
public sealed class HealthInsightDto
{
    /// <summary>Síntoma con mayor promedio de dolor registrado en el período. Null si no hay datos de dolor.</summary>
    public string? SintomaConMayorDolor { get; init; }

    /// <summary>Promedio de dolor del síntoma más intenso (0–10). Null si no hay datos.</summary>
    public double? PromedioDolorMax { get; init; }

    /// <summary>Promedio global de dolor de todos los trackings con valor registrado. Null si no hay datos.</summary>
    public double? PromedioDolorGlobal { get; init; }

    /// <summary>Cantidad de registros que reportaron dolor ≥ 7 (dolor alto).</summary>
    public int RegistrosDolorAlto { get; init; }

    /// <summary>Síntoma con más trackings en el período.</summary>
    public string? SintomaFrecuente { get; init; }

    /// <summary>Cantidad de trackings del síntoma más frecuente.</summary>
    public int SintomaFrecuenteConteo { get; init; }

    /// <summary>Síntoma con mayor frecuencia promedio registrada (catálogo). Null si no hay datos.</summary>
    public string? SintomaMayorFrecuencia { get; init; }

    /// <summary>Nombre de la frecuencia más común del síntoma con mayor frecuencia promedio.</summary>
    public string? SintomaMayorFrecuenciaNombre { get; init; }

    /// <summary>Indica si hay datos suficientes para mostrar insights (al menos 1 tracking).</summary>
    public bool TieneDatos { get; init; }
}
