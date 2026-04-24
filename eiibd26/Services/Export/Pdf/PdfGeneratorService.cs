using eiibd26.DTOs.Analytics;
using eiibd26.DTOs.Export;
using eiibd26.Services.Analytics;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace eiibd26.Services.Export.Pdf;

public sealed class PdfGeneratorService : IPdfGeneratorService
{
    private const string TituloDocumento = "EIIBD — Resumen de Salud Personal";
    private const float  TamFuente       = 9f;
    private const string Negro           = "#000000";
    private const string GrisOscuro     = "#333333";
    private const string GrisMedio      = "#666666";
    private const string GrisClaro      = "#999999";

    private readonly IHealthStatsService _stats;

    public PdfGeneratorService(IHealthStatsService stats)
    {
        _stats = stats;
    }

    public byte[] GenerarPdf(MedicalSummaryDto data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var stats = _stats.Calcular(data.EstadosAnimo, data.Sintomas);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(2.2f, Unit.Centimetre);
                page.MarginTop(1.8f, Unit.Centimetre);
                page.MarginBottom(3.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(TamFuente).FontFamily("Arial").FontColor(GrisOscuro));

                page.Header().Element(c => RenderHeader(c, data));
                page.Content().Element(c => RenderContent(c, data, stats));
                page.Footer().Element(c => RenderFooter(c, data));
            });
        }).GeneratePdf();
    }

    // ── HEADER ────────────────────────────────────────────────────────────────

    private static void RenderHeader(IContainer container, MedicalSummaryDto data)
    {
        container.Column(col =>
        {
            // Página 1: header completo
            col.Item().ShowOnce().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("Resumen de Salud Personal")
                        .Bold().FontSize(TamFuente).FontColor(Negro);
                    left.Item().Text($"Generado el {DateTime.Today:dd/MM/yyyy}")
                        .FontSize(TamFuente - 1).FontColor(GrisMedio);
                });

                row.ConstantItem(220).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text($"Paciente: {data.Perfil.NombreCompleto}")
                        .FontSize(TamFuente).FontColor(GrisOscuro);
                    right.Item().AlignRight().Text($"Período: {data.Desde:dd/MM/yyyy} — {data.Hasta:dd/MM/yyyy}")
                        .FontSize(TamFuente).FontColor(GrisMedio);
                });
            });

            // Páginas siguientes: header compacto
            col.Item().SkipOnce().Text($"Resumen de Salud Personal — {data.Perfil.NombreCompleto}")
                .FontSize(TamFuente).FontColor(GrisMedio);

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Negro);
        });
    }

    // ── FOOTER ────────────────────────────────────────────────────────────────

    private static void RenderFooter(IContainer container, MedicalSummaryDto data)
    {
        container.Column(col =>
        {
            col.Item().Column(aviso =>
            {
                aviso.Item().Text("Aviso Importante")
                    .Bold().FontSize(TamFuente - 1).FontColor(Negro);

                aviso.Item().PaddingTop(3).Text(
                    "© 2026 eiibd. Todos los derechos reservados. " +
                    "Ninguna información en eiibd debe interpretarse como consejo médico, diagnóstico o tratamiento. " +
                    "El contenido se proporciona solo con fines informativos y no debe utilizarse para sustituir la consulta con un profesional de la salud calificado.")
                    .FontSize(TamFuente - 1).FontColor(GrisMedio);

                aviso.Item().PaddingTop(4).Text("Más información en:")
                    .FontSize(TamFuente - 1).FontColor(GrisMedio);
                aviso.Item().Text("https://eiibd.com")
                    .FontSize(TamFuente - 1).FontColor(GrisMedio);
            });

            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Negro);

            col.Item().PaddingTop(4).AlignRight().Text(t =>
            {
                t.Span("Página ").FontSize(TamFuente).FontColor(GrisMedio);
                t.CurrentPageNumber().FontSize(TamFuente).FontColor(GrisMedio);
                t.Span(" de ").FontSize(TamFuente).FontColor(GrisMedio);
                t.TotalPages().FontSize(TamFuente).FontColor(GrisMedio);
            });
        });
    }

    // ── CONTENT ───────────────────────────────────────────────────────────────

    private static void RenderContent(IContainer container, MedicalSummaryDto data, HealthStatsDto stats)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(0);

            if (data.DatosTruncados)
            {
                col.Item().PaddingBottom(8).Text(
                    $"Nota: Se muestran los primeros {data.LimiteAplicado} registros del período. " +
                    "Ajusta el rango de fechas para ver datos adicionales.")
                    .FontSize(TamFuente).Italic().FontColor(GrisMedio);
            }

            // Resumen estadístico al inicio — lo primero que ve el médico
            col.Item().Element(c => BloqueResumenPeriodo(c, stats));
            Separador(col);

            // Bloques en orden: perfil, condiciones, tratamientos, síntomas, estado de ánimo
            col.Item().Element(c => BloquePerfil(c, data));
            Separador(col);

            if (data.Condiciones.Count > 0 || true)
            {
                col.Item().Element(c => BloqueCondiciones(c, data));
                Separador(col);
            }

            if (data.Tratamientos.Count > 0)
            {
                col.Item().Element(c => BloqueTratamientos(c, data.Tratamientos));
                Separador(col);
            }

            if (data.Sintomas.Count > 0)
            {
                col.Item().Element(c => BloqueSintomas(c, data.Sintomas));
                Separador(col);
            }

            if (data.EstadosAnimo.Count > 0)
            {
                col.Item().Element(c => BloqueEstadosAnimo(c, data.EstadosAnimo));
                Separador(col);
            }

            if (data.EstadosAnimo.Count == 0 && data.Sintomas.Count == 0 && data.Tratamientos.Count == 0)
            {
                col.Item().PaddingTop(8).Text("No se encontraron registros en el período seleccionado.")
                    .FontSize(TamFuente).Italic().FontColor(GrisMedio);
            }

            // Sección médico al final
            col.Item().Element(BloqueSeccionMedico);
        });
    }

    // ── SEPARADOR ENTRE BLOQUES ───────────────────────────────────────────────

    private static void Separador(ColumnDescriptor col)
    {
        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Negro);
    }

    // ── TÍTULO DE BLOQUE ─────────────────────────────────────────────────────

    private static void TituloBloque(IContainer container, string titulo)
    {
        container.Column(col =>
        {
            col.Item().Text(titulo).Bold().FontSize(TamFuente).FontColor(Negro);
            col.Item().PaddingTop(3).LineHorizontal(1).LineColor(Negro);
        });
    }

    // ── BLOQUE: PERFIL ────────────────────────────────────────────────────────

    private static void BloquePerfil(IContainer container, MedicalSummaryDto data)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Información del Paciente"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(3);

                var edad = data.Perfil.FechaDeNacimiento.HasValue
                    ? $"{CalcularEdad(data.Perfil.FechaDeNacimiento.Value)} años  ({data.Perfil.FechaDeNacimiento.Value:dd/MM/yyyy})"
                    : "—";
                var ubicacion = string.Join(", ",
                    new[] { data.Perfil.NombreCiudad, data.Perfil.NombrePais }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                Linea(body, "Nombre", data.Perfil.NombreCompleto);
                Linea(body, "Género", data.Perfil.Genero ?? "—");
                Linea(body, "Fecha de Nacimiento / Edad", edad);
                Linea(body, "Ciudad / País", string.IsNullOrEmpty(ubicacion) ? "—" : ubicacion);
            });
        });
    }

    // ── BLOQUE: CONDICIONES ───────────────────────────────────────────────────

    private static void BloqueCondiciones(IContainer container, MedicalSummaryDto data)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Condiciones"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(3);
                if (data.Condiciones.Count == 0)
                {
                    body.Item().Text("Sin condiciones registradas.").FontSize(TamFuente).Italic().FontColor(GrisMedio);
                }
                else
                {
                    foreach (var c in data.Condiciones)
                    {
                        var tiempoTexto = c.FechaInicio.HasValue
                            ? TiempoTranscurrido(c.FechaInicio.Value)
                            : null;
                        var texto = tiempoTexto != null
                            ? $"{c.Nombre} — {tiempoTexto}"
                            : c.Nombre;
                        body.Item().Text(texto).FontSize(TamFuente).FontColor(GrisOscuro);
                    }
                }
            });
        });
    }

    // ── BLOQUE: ESTADO DE ÁNIMO ───────────────────────────────────────────────

    private static void BloqueEstadosAnimo(IContainer container, List<EstadoAnimoExportDto> estados)
    {
        container.Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Estado de Ánimo"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(3);
                foreach (var e in estados)
                {
                    var nota = string.IsNullOrWhiteSpace(e.Texto) ? "Sin notas" : e.Texto;
                    body.Item().Text(
                        $"{e.FechaRegistro:dd/MM/yyyy}  —  {TraducirAnimo(e.EstadoMoodNombre)}  —  {nota}")
                        .FontSize(TamFuente).FontColor(GrisOscuro);
                }
            });
        });
    }

    // ── BLOQUE: SÍNTOMAS ─────────────────────────────────────────────────────

    private static void BloqueSintomas(IContainer container, List<SintomaExportDto> sintomas)
    {
        container.Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Seguimiento de Síntomas"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(10);
                foreach (var s in sintomas)
                {
                    body.Item().ShowEntire().Column(bloque =>
                    {
                        bloque.Item().Text(s.NombreSintoma).Bold().FontSize(TamFuente).FontColor(Negro);
                        bloque.Item().PaddingTop(6).Column(trackings =>
                        {
                            trackings.Spacing(2);
                            foreach (var trk in s.Trackings)
                            {
                                var estadoTexto = string.Equals(trk.Estado, "Ninguno", StringComparison.OrdinalIgnoreCase)
                                    ? "Sin síntomas" : trk.Estado;
                                trackings.Item().Text($"{trk.Fecha:dd/MM/yyyy} — {estadoTexto}")
                                    .FontSize(TamFuente).FontColor(GrisOscuro);
                            }
                        });
                    });
                }
            });
        });
    }

    // ── BLOQUE: TRATAMIENTOS ─────────────────────────────────────────────────

    private static void BloqueTratamientos(IContainer container, List<TratamientoExportDto> tratamientos)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Tratamientos"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(3);
                foreach (var tr in tratamientos)
                {
                    body.Item().Text($"{tr.Nombre} — Desde {tr.FechaInicio:dd/MM/yyyy}")
                        .FontSize(TamFuente).FontColor(GrisOscuro);
                }
            });
        });
    }

    // ── BLOQUE: RESUMEN DEL PERÍODO ───────────────────────────────────────────

    private static void BloqueResumenPeriodo(IContainer container, HealthStatsDto stats)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Resumen del Período"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(3);
                body.Item().Text(t =>
                {
                    t.Span("Estado de ánimo: ").Bold().FontSize(TamFuente).FontColor(GrisMedio);
                    t.Span($"{stats.Mood.Promedio} ({stats.Mood.TotalRegistros} registros)")
                        .FontSize(TamFuente).FontColor(GrisOscuro);
                });
                body.Item().Text(t =>
                {
                    t.Span("Tendencia de síntomas: ").Bold().FontSize(TamFuente).FontColor(GrisMedio);
                    t.Span($"{stats.Symptoms.Tendencia} ({stats.Symptoms.TotalRegistros} registros)")
                        .FontSize(TamFuente).FontColor(GrisOscuro);
                });
            });
        });
    }

    // ── BLOQUE: SECCIÓN MÉDICO ────────────────────────────────────────────────

    private static void BloqueSeccionMedico(IContainer container)
    {
        container.ShowEntire().Column(col =>
        {
            col.Item().Element(c => TituloBloque(c, "Para uso del Médico"));
            col.Item().PaddingTop(6).Column(body =>
            {
                body.Spacing(10);
                body.Item().Text(t =>
                {
                    t.Span("Nombre del Médico: ").Bold().FontSize(TamFuente).FontColor(GrisMedio);
                    t.Span("________________________").FontSize(TamFuente).FontColor(GrisClaro);
                });
                body.Item().Text("Comentarios:").FontSize(TamFuente).FontColor(GrisMedio);
                body.Item().PaddingTop(8).LineHorizontal(1).LineColor(Negro);
                body.Item().PaddingTop(14).LineHorizontal(1).LineColor(Negro);
                body.Item().PaddingTop(14).LineHorizontal(1).LineColor(Negro);
            });
        });
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    /// <summary>Fila etiqueta: valor en una sola línea.</summary>
    private static void Linea(ColumnDescriptor col, string etiqueta, string valor)
    {
        col.Item().Text(t =>
        {
            t.Span($"{etiqueta}: ").Bold().FontSize(TamFuente).FontColor(GrisMedio);
            t.Span(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(TamFuente).FontColor(GrisOscuro);
        });
    }

    private static string TiempoTranscurrido(DateTime fecha)
    {
        var hoy   = DateTime.Today;
        var años  = hoy.Year - fecha.Year;
        var meses = hoy.Month - fecha.Month;
        if (hoy.Day < fecha.Day)
            meses--;
        if (meses < 0) { años--; meses += 12; }
        if (años > 0)
            return meses > 0 ? $"{años} año{(años > 1 ? "s" : "")} y {meses} mes{(meses > 1 ? "es" : "")}" : $"{años} año{(años > 1 ? "s" : "")}";
        return meses > 0 ? $"{meses} mes{(meses > 1 ? "es" : "")}" : "menos de un mes";
    }

    private static string TraducirAnimo(string nombre) => nombre switch
    {
        "MuyBien"  => "Muy bien",
        "Bien"     => "Bien",
        "Neutral"  => "Neutral",
        "Mal"      => "Mal",
        "MuyMal"   => "Muy mal",
        _          => nombre
    };

    private static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        var edad = hoy.Year - fechaNacimiento.Year;
        if (hoy < fechaNacimiento.AddYears(edad))
            edad--;
        return edad;
    }
}
