using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services.Analytics;
using eiibd26.DTOs.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eiibd26.Pages.u
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndexModel> _logger;
        private readonly IHealthStatsService _stats;
        private readonly IHealthInsightService _insights;

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger,
            IHealthStatsService stats, IHealthInsightService insights)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _insights = insights ?? throw new ArgumentNullException(nameof(insights));
        }

        public PerfilPublicoVm PerfilPublico { get; set; }
        public bool PerfilIncompleto { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Slug vacío en solicitud de perfil público");
                return NotFound();
            }

            try
            {
                // Primero buscamos el perfil por slug sin filtrar por Nombre para detectar
                // perfiles existentes pero incompletos.
                var perfil = await _db.Perfil
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.slug == slug);

                if (perfil == null)
                {
                    _logger.LogInformation("Perfil no encontrado para slug: {Slug}", slug);
                    return NotFound();
                }

                if (string.IsNullOrWhiteSpace(perfil.Nombre))
                {
                    // Perfil existe pero no tiene nombre (incompleto). Mostramos la misma página
                    // pero con un aviso al usuario para indicar que no es posible conocer al usuario.
                    PerfilIncompleto = true;
                    PerfilPublico = null;
                    return Page();
                }

                // Determinar fecha de registro
                DateTime fechaRegistro;
                if (perfil.FechaCreacion != default(DateTime) && perfil.FechaCreacion > DateTime.MinValue)
                {
                    fechaRegistro = perfil.FechaCreacion;
                }
                else if (perfil.FechaCreado.HasValue && perfil.FechaCreado.Value != default(DateTime) && perfil.FechaCreado.Value > DateTime.MinValue)
                {
                    fechaRegistro = perfil.FechaCreado.Value;
                }
                else
                {
                    fechaRegistro = DateTime.UtcNow;
                }

                // V-003: Only load and expose medical data when the user explicitly opted in.
                var comparteDatosMedicos = perfil.PermitirCompartirDatosMedicos == true;

                // Ventana de 1 mes, igual que «Exportar 1 mes» del dashboard
                var hasta = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1); // fin del día de hoy
                var desde = DateTime.UtcNow.Date.AddMonths(-1);

                var condiciones = comparteDatosMedicos
                    ? await _db.condicionUsuario
                        .AsNoTracking()
                        .Where(c => c.idUsuario == perfil.idUser && !c.Eliminado)
                        .Include(c => c.Condicion)
                        .OrderByDescending(c => c.fechaCreado)
                        .Select(c => new InfoClinicaVm
                        {
                            Nombre = c.Condicion.nombre ?? "Sin nombre",
                            Icono = c.Condicion.icono,
                            FechaInicio = c.fechaInicio,
                            EdadDiagnostico = null
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                var sintomas = comparteDatosMedicos
                    ? await _db.sintomasUsuario
                        .AsNoTracking()
                        .Where(s => s.idUsuario == perfil.idUser && !s.Eliminado)
                        .Include(s => s.Sintoma)
                        .OrderByDescending(s => s.fechaCreado)
                        .Select(s => new InfoClinicaVm
                        {
                            Nombre = s.Sintoma.nombre ?? "Sin nombre",
                            Icono = s.Sintoma.icono,
                            FechaInicio = s.fechaInicio
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                // Cargar tratamientos
                var tratamientos = comparteDatosMedicos
                    ? await _db.tratamientoUsuario
                        .AsNoTracking()
                        .Where(t => t.idUsuario == perfil.idUser && !t.Eliminado)
                        .Include(t => t.Tratamiento)
                        .OrderByDescending(t => t.fechaCreado)
                        .Select(t => new InfoClinicaVm
                        {
                            Nombre = t.Tratamiento.nombre ?? "Sin nombre",
                            Icono = t.Tratamiento.icono,
                            FechaInicio = t.fechaInicio
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                // Cargar todos los trackings de síntomas con campos completos
                List<TrackingSintomaVm> trackingSintomas;
                List<SintomaConTrackingVm> sintomasAgrupados;
                List<TrackingSintomaUsuario> rawTrackings = new();
                Dictionary<int, List<string>> mapaTratamientos = new();

                if (comparteDatosMedicos)
                {
                    rawTrackings = await _db.TrackingSintomaUsuario
                        .AsNoTracking()
                        .Where(t => t.IdUsuario == perfil.idUser && t.Fecha >= desde && t.Fecha <= hasta)
                        .Include(t => t.SintomaUsuario).ThenInclude(s => s.Sintoma)
                        .Include(t => t.Frecuencia)
                        .OrderByDescending(t => t.Fecha)
                        .ToListAsync();

                    // Flat list (últimos 10) para el grid resumen
                    trackingSintomas = rawTrackings
                        .Take(10)
                        .Select(t => new TrackingSintomaVm
                        {
                            SintomaNombre    = t.SintomaUsuario?.Sintoma?.nombre ?? "Sin nombre",
                            Estado           = t.Estado,
                            Fecha            = t.Fecha,
                            Dolor            = t.Dolor,
                            TieneSangrado    = t.TieneSangrado,
                            FrecuenciaNombre = t.Frecuencia?.Nombre
                        })
                        .ToList();

                    // Los tratamientos se resuelven sobre todo el mes (rawTrackings completo)
                    var idsSintomasUsuario = rawTrackings
                        .Where(t => t.SintomaUsuario != null)
                        .Select(t => t.IdSintomaUsuario)
                        .Distinct()
                        .ToList();

                    var tratamientosPorSintoma = await _db.TratamientoSintomaUsuario
                        .AsNoTracking()
                        .Where(ts => ts.IdSintomaUsuario != null && idsSintomasUsuario.Contains(ts.IdSintomaUsuario.Value))
                        .Include(ts => ts.TratamientoUsuario).ThenInclude(tu => tu.Tratamiento)
                        .ToListAsync();

                    mapaTratamientos = tratamientosPorSintoma
                        .Where(ts => ts.TratamientoUsuario?.Tratamiento != null)
                        .GroupBy(ts => ts.IdSintomaUsuario!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(ts => ts.TratamientoUsuario!.Tratamiento!.nombre)
                                   .Distinct().OrderBy(n => n).ToList());

                    // Solo los últimos 10 registros se muestran en pantalla (agrupados por síntoma)
                    var top10Trackings = rawTrackings.Take(10).ToList();

                    sintomasAgrupados = top10Trackings
                        .Where(t => t.SintomaUsuario?.Sintoma != null)
                        .GroupBy(t => new { t.IdSintomaUsuario, NombreSintoma = t.SintomaUsuario!.Sintoma!.nombre })
                        .Select(g => new SintomaConTrackingVm
                        {
                            NombreSintoma = g.Key.NombreSintoma,
                            Tratamientos  = mapaTratamientos.TryGetValue(g.Key.IdSintomaUsuario, out var trts) ? trts : new(),
                            Trackings     = g.OrderByDescending(t => t.Fecha).Select(t => new TrackingSintomaVm
                            {
                                SintomaNombre    = g.Key.NombreSintoma,
                                Estado           = t.Estado,
                                Fecha            = t.Fecha,
                                Dolor            = t.Dolor,
                                TieneSangrado    = t.TieneSangrado,
                                FrecuenciaNombre = t.Frecuencia?.Nombre
                            }).ToList()
                        })
                        .OrderBy(s => s.NombreSintoma)
                        .ToList();
                }
                else
                {
                    trackingSintomas = new List<TrackingSintomaVm>();
                    sintomasAgrupados = new List<SintomaConTrackingVm>();
                }

                // Cargar estados de ánimo del último mes (mismo rango que la exportación)
                var estadosAnimo = comparteDatosMedicos
                    ? await _db.EstadoAnimoUsuario
                        .AsNoTracking()
                        .Where(e => e.IdUsuario == perfil.idUser && !e.Eliminado
                                    && e.FechaRegistro >= desde && e.FechaRegistro <= hasta)
                        .Include(e => e.CondicionUsuario).ThenInclude(c => c.Condicion)
                        .Include(e => e.SintomaUsuario).ThenInclude(s => s.Sintoma)
                        .Include(e => e.TratamientoUsuario).ThenInclude(t => t.Tratamiento)
                        .OrderByDescending(e => e.FechaRegistro)
                        .Take(10)
                        .Select(e => new EstadoAnimoVm
                        {
                            Estado = (int)e.EstadoMood,
                            Texto = e.Texto,
                            FechaRegistro = e.FechaRegistro,
                            CondicionNombre = e.CondicionUsuario != null ? e.CondicionUsuario.Condicion.nombre : null,
                            SintomaNombre = e.SintomaUsuario != null ? e.SintomaUsuario.Sintoma.nombre : null,
                            TratamientoNombre = e.TratamientoUsuario != null ? e.TratamientoUsuario.Tratamiento.nombre : null
                        })
                        .ToListAsync()
                    : new List<EstadoAnimoVm>();

                // Calcular estadísticas e insights sobre el mes completo (rawTrackings), no solo los top-10
                HealthResumenVm? healthResumen = null;
                if (comparteDatosMedicos && sintomasAgrupados.Count > 0)
                {
                    // Reconstruir los síntomas con TODOS los trackings del mes para el motor analítico
                    var sintomasExportDto = rawTrackings
                        .Where(t => t.SintomaUsuario?.Sintoma != null)
                        .GroupBy(t => new { t.IdSintomaUsuario, NombreSintoma = t.SintomaUsuario!.Sintoma!.nombre })
                        .Select(g => new SintomaExportDto
                        {
                            NombreSintoma = g.Key.NombreSintoma,
                            Tratamientos  = mapaTratamientos.TryGetValue(g.Key.IdSintomaUsuario, out var trts) ? trts : new(),
                            Trackings     = g.OrderBy(t => t.Fecha).Select(t => new TrackingSintomaExportDto
                            {
                                Fecha            = t.Fecha,
                                Estado           = t.Estado,
                                Dolor            = t.Dolor,
                                TieneSangrado    = t.TieneSangrado,
                                FrecuenciaNombre = t.Frecuencia?.Nombre
                            }).ToList()
                        }).ToList();

                    // Cargar TODOS los estados de ánimo del período para analytics (sin Take),
                    // igual que MedicalSummaryService. estadosAnimo (Top 10) sigue siendo solo para UI.
                    var estadosAnimoFull = await _db.EstadoAnimoUsuario
                        .AsNoTracking()
                        .Where(e => e.IdUsuario == perfil.idUser && !e.Eliminado
                                    && e.FechaRegistro >= desde && e.FechaRegistro <= hasta)
                        .OrderBy(e => e.FechaRegistro)
                        .Select(e => new EstadoAnimoExportDto
                        {
                            FechaRegistro    = e.FechaRegistro,
                            EstadoMood       = (int)e.EstadoMood,
                            EstadoMoodNombre = e.EstadoMood.ToString(),
                            Texto            = e.Texto
                        })
                        .ToListAsync();

                    var estadosExportDto = estadosAnimoFull;

                    var statsDto   = _stats.Calcular(estadosExportDto, sintomasExportDto);
                    var insightDto = _insights.Analizar(sintomasExportDto);

                    healthResumen = new HealthResumenVm
                    {
                        MoodPromedio            = statsDto.Mood.Promedio,
                        MoodTotalRegistros      = statsDto.Mood.TotalRegistros,
                        TendenciaGeneral        = statsDto.Symptoms.Tendencia,
                        TendenciaTotalRegistros = statsDto.Symptoms.TotalRegistros,
                        TendenciaPorSintoma     = statsDto.Symptoms.PorSintoma
                                                    .Select(t => (t.NombreSintoma, t.Tendencia)).ToList(),
                        SintomaFrecuente       = insightDto.SintomaFrecuente,
                        SintomaFrecuenteConteo = insightDto.SintomaFrecuenteConteo,
                        PromedioDolorGlobal    = insightDto.PromedioDolorGlobal,
                        SintomaConMayorDolor   = insightDto.SintomaConMayorDolor,
                        PromedioDolorMax       = insightDto.PromedioDolorMax,
                        RegistrosDolorAlto     = insightDto.RegistrosDolorAlto,
                        SintomaMayorFrecuencia       = insightDto.SintomaMayorFrecuencia,
                        SintomaMayorFrecuenciaNombre = insightDto.SintomaMayorFrecuenciaNombre,
                        TieneInsights          = insightDto.TieneDatos
                    };
                }

                PerfilPublico = new PerfilPublicoVm
                {
                    Desde = desde,
                    Hasta = hasta,
                    Slug = perfil.slug,
                    NombreCompleto = $"{perfil.Nombre} {perfil.Apellidos}".Trim(),
                    Avatar = string.IsNullOrWhiteSpace(perfil.Avatar)
                        ? $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(perfil.Nombre ?? "Usuario")}&size=140"
                        : perfil.Avatar,
                    AcercaDe = perfil.AcercaDe,
                    EstoyAquiTexto = ObtenerEstoyAquiTexto(perfil.EstoyAqui),
                    GeneroTexto = perfil.Genero,
                    EdadAproximada = CalcularEdad(perfil.FechaDeNacimiento),
                    UbicacionTexto = perfil.PermitirMostrarPais == true ? ObtenerUbicacion(perfil) : null,
                    MostrarUbicacion = perfil.PermitirMostrarPais == true,
                    MostrarDatosMedicos = comparteDatosMedicos,
                    FechaRegistro = fechaRegistro,
                    Condiciones = condiciones,
                    Sintomas = sintomas, // Ya no se usa en el grid
                    Tratamientos = tratamientos,
                    EstadosAnimo = estadosAnimo,
                    TrackingSintomas  = trackingSintomas,
                    SintomasAgrupados = sintomasAgrupados,
                    HealthResumen     = healthResumen
                };

                // Compute EdadDiagnostico for condiciones using perfil.FechaDeNacimiento
                if (perfil.FechaDeNacimiento.HasValue)
                {
                    foreach (var cond in PerfilPublico.Condiciones)
                    {
                        cond.EdadDiagnostico = Helpers.DateCalculationHelper
                            .CalcularEdadAlDiagnostico(perfil.FechaDeNacimiento.Value, cond.FechaInicio);
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar perfil público para slug: {Slug}", slug);
                return NotFound();
            }
        }

        private string ObtenerEstoyAquiTexto(int? estoyAqui)
        {
            return estoyAqui switch
            {
                1 => "Estoy aquí por mí",
                2 => "Estoy aquí por mi hijo",
                3 => "Estoy aquí por mi hija",
                4 => "Estoy aquí por mi papá",
                5 => "Estoy aquí por mi mamá",
                6 => "Estoy aquí por mi hermano",
                7 => "Estoy aquí por mi hermana",
                8 => "Estoy aquí por un familiar",
                9 => "Estoy aquí por alguien más",
                _ => null
            };
        }

        private int? CalcularEdad(DateTime? fechaNacimiento)
        {
            if (!fechaNacimiento.HasValue || fechaNacimiento.Value > DateTime.Today)
                return null;

            var edad = DateTime.Today.Year - fechaNacimiento.Value.Year;
            if (DateTime.Today < fechaNacimiento.Value.AddYears(edad))
                edad--;

            return edad > 0 && edad < 120 ? edad : null;
        }

        private string ObtenerUbicacion(Perfil perfil)
        {
            if (!string.IsNullOrWhiteSpace(perfil.NombreCiudad) && !string.IsNullOrWhiteSpace(perfil.NombrePais))
            {
                return $"{perfil.NombreCiudad}, {perfil.NombrePais}";
            }
            else if (!string.IsNullOrWhiteSpace(perfil.NombrePais))
            {
                return perfil.NombrePais;
            }
            else if (!string.IsNullOrWhiteSpace(perfil.NombreCiudad))
            {
                return perfil.NombreCiudad;
            }
            return null;
        }
    }

    public class PerfilPublicoVm
    {
        public string Slug { get; set; }
        public string NombreCompleto { get; set; }
        public string Avatar { get; set; }
        public string AcercaDe { get; set; }
        public string EstoyAquiTexto { get; set; }
        public string GeneroTexto { get; set; }
        public int? EdadAproximada { get; set; }
        public string UbicacionTexto { get; set; }
        public bool MostrarUbicacion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool MostrarDatosMedicos { get; set; }

        // Período de la BIO pública (siempre los últimos 30 días, igual que «Exportar 1 mes» del dashboard)
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }

        // Información clínica
        public List<InfoClinicaVm> Condiciones { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Sintomas { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Tratamientos { get; set; } = new List<InfoClinicaVm>();
        public List<EstadoAnimoVm> EstadosAnimo { get; set; } = new List<EstadoAnimoVm>();
        public List<TrackingSintomaVm> TrackingSintomas { get; set; } = new List<TrackingSintomaVm>(); // ✅ NUEVO
        public List<SintomaConTrackingVm> SintomasAgrupados { get; set; } = new List<SintomaConTrackingVm>();
        public HealthResumenVm? HealthResumen { get; set; }
    }

    public class InfoClinicaVm
    {
        public string Nombre { get; set; }
        public string Icono { get; set; }
        public DateTime? FechaInicio { get; set; }
        // Edad (años) al momento del diagnóstico (calculada a partir de FechaInicio y FechaDeNacimiento del perfil)
        public int? EdadDiagnostico { get; set; }
    }

    public class EstadoAnimoVm
    {
        public int Estado { get; set; }
        public string Texto { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string CondicionNombre { get; set; }
        public string SintomaNombre { get; set; }
        public string TratamientoNombre { get; set; }

        public string ImagenEstado => Estado switch
        {
            5 => "/img/muybien.svg",
            4 => "/img/bien.svg",
            3 => "/img/neutral.svg",
            2 => "/img/mal.svg",
            1 => "/img/muymal.svg",
            _ => "/img/neutral.svg"
        };

        public string ColorEstado => Estado switch
        {
            5 => "#38D6C1",
            4 => "#B3F1E9",
            3 => "#FEE019",
            2 => "#D8B4F8",
            1 => "#9B5DE5",
            _ => "#9ca3af"
        };

        public string TextoEstado => Estado switch
        {
            5 => "Muy bien",
            4 => "Bien",
            3 => "Neutral",
            2 => "Mal",
            1 => "Muy mal",
            _ => "Desconocido"
        };
    }

    // ✅ NUEVO: ViewModel para Tracking de Síntomas
    public class TrackingSintomaVm
    {
        public string SintomaNombre { get; set; }
        public string Estado { get; set; } // "Ninguno", "Leve", "Moderado", "Severo"
        public DateTime Fecha { get; set; }
        public int? Dolor { get; set; }
        public bool? TieneSangrado { get; set; }
        public string? FrecuenciaNombre { get; set; }

        public string ColorEstado => Estado switch
        {
            // Normalizado para coincidir con los colores usados en los partials de seguimiento
            // (ver Areas/Identity/Pages/Usuario/_SintomasTop5Seguimiento.cshtml)
            "Severo" => "#FF8A65",
            "Extremo" => "#D32F2F",
            "Moderado" => "#FFD121",
            "Leve" => "#B6C2D7",
            "Ninguno" => "#E6E6EB",
            _ => "#9ca3af"
        };

        public string IconoEstado => Estado switch
        {
            "Severo" => "bi-exclamation-triangle-fill",
            "Extremo" => "bi-exclamation-octagon-fill",
            "Moderado" => "bi-exclamation-circle-fill",
            "Leve" => "bi-info-circle-fill",
            "Ninguno" => "bi-check-circle-fill",
            _ => "bi-circle"
        };
    }

    /// <summary>Síntoma del usuario con todos sus trackings agrupados (equivalente a SintomaExportDto del PDF).</summary>
    public class SintomaConTrackingVm
    {
        public string NombreSintoma { get; set; } = string.Empty;
        public List<string> Tratamientos { get; set; } = new();
        public List<TrackingSintomaVm> Trackings { get; set; } = new();
    }

    /// <summary>Resumen estadístico e insights equivalente al bloque inicial del PDF.</summary>
    public class HealthResumenVm
    {
        // Mood
        public string MoodPromedio { get; set; } = string.Empty;
        public int MoodTotalRegistros { get; set; }
        // Tendencia síntomas
        public string TendenciaGeneral { get; set; } = string.Empty;
        public int TendenciaTotalRegistros { get; set; }
        public List<(string Sintoma, string Tendencia)> TendenciaPorSintoma { get; set; } = new();
        // Insights
        public string? SintomaFrecuente { get; set; }
        public int SintomaFrecuenteConteo { get; set; }
        public double? PromedioDolorGlobal { get; set; }
        public string? SintomaConMayorDolor { get; set; }
        public double? PromedioDolorMax { get; set; }
        public int RegistrosDolorAlto { get; set; }
        public string? SintomaMayorFrecuencia { get; set; }
        public string? SintomaMayorFrecuenciaNombre { get; set; }
        public bool TieneInsights { get; set; }
    }
}