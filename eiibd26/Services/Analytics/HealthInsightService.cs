using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;

namespace eiibd26.Services.Analytics;

/// <summary>
/// Genera insights clínicos básicos a partir de listas de síntomas ya consultadas.
/// Servicio sin acceso a base de datos; puede utilizarse en dashboard y PDF.
/// </summary>
public sealed class HealthInsightService : IHealthInsightService
{
    private const int UmbralDolorAlto = 7;

    public HealthInsightDto Analizar(List<SintomaExportDto> sintomas)
    {
        if (sintomas is not { Count: > 0 })
            return new HealthInsightDto { TieneDatos = false };

        // ── Síntoma más frecuente ─────────────────────────────────────────────
        var masFrecuente = sintomas
            .Select(s => new { s.NombreSintoma, Conteo = s.Trackings.Count })
            .OrderByDescending(x => x.Conteo)
            .FirstOrDefault();

        // ── Datos de dolor ────────────────────────────────────────────────────
        // Aplanar todos los trackings con dolor registrado
        var trackingsConDolor = sintomas
            .SelectMany(s => s.Trackings
                .Where(t => t.Dolor.HasValue && t.Dolor.Value > 0)
                .Select(t => new { s.NombreSintoma, t.Dolor }))
            .ToList();

        double? promedioGlobal = trackingsConDolor.Count > 0
            ? trackingsConDolor.Average(x => (double)x.Dolor!.Value)
            : null;

        int registrosAltos = trackingsConDolor.Count(x => x.Dolor!.Value >= UmbralDolorAlto);

        // Síntoma con mayor promedio de dolor
        string? sintomaDolorMax = null;
        double? promedioDolorMax = null;

        if (trackingsConDolor.Count > 0)
        {
            var porSintoma = trackingsConDolor
                .GroupBy(x => x.NombreSintoma)
                .Select(g => new { Nombre = g.Key, Promedio = g.Average(x => (double)x.Dolor!.Value) })
                .OrderByDescending(x => x.Promedio)
                .First();

            sintomaDolorMax = porSintoma.Nombre;
            promedioDolorMax = Math.Round(porSintoma.Promedio, 1);
        }

        // ── Síntoma con mayor frecuencia registrada (catálogo) ───────────────
        // Usamos el nombre de frecuencia más repetido por síntoma y elegimos el que más veces la registró.
        string? sintomaMayorFrecuencia = null;
        string? sintomaMayorFrecuenciaNombre = null;

        var trackingsConFrecuencia = sintomas
            .SelectMany(s => s.Trackings
                .Where(t => !string.IsNullOrWhiteSpace(t.FrecuenciaNombre))
                .Select(t => new { s.NombreSintoma, t.FrecuenciaNombre }))
            .ToList();

        if (trackingsConFrecuencia.Count > 0)
        {
            var porSintoma = trackingsConFrecuencia
                .GroupBy(x => x.NombreSintoma)
                .Select(g => new
                {
                    Nombre = g.Key,
                    Conteo = g.Count(),
                    FrecuenciaMasComun = g
                        .GroupBy(x => x.FrecuenciaNombre)
                        .OrderByDescending(fg => fg.Count())
                        .First().Key
                })
                .OrderByDescending(x => x.Conteo)
                .First();

            sintomaMayorFrecuencia      = porSintoma.Nombre;
            sintomaMayorFrecuenciaNombre = porSintoma.FrecuenciaMasComun;
        }

        return new HealthInsightDto
        {
            TieneDatos               = true,
            SintomaFrecuente         = masFrecuente?.NombreSintoma,
            SintomaFrecuenteConteo   = masFrecuente?.Conteo ?? 0,
            PromedioDolorGlobal      = promedioGlobal.HasValue ? Math.Round(promedioGlobal.Value, 1) : null,
            RegistrosDolorAlto       = registrosAltos,
            SintomaConMayorDolor     = sintomaDolorMax,
            PromedioDolorMax         = promedioDolorMax,
            SintomaMayorFrecuencia      = sintomaMayorFrecuencia,
            SintomaMayorFrecuenciaNombre = sintomaMayorFrecuenciaNombre,
        };
    }
}
