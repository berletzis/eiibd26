using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eiibd26.Data;
using eiibd26.Models;
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

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                // Cargar últimas 3 condiciones
                // V-003: Only load and expose medical data when the user explicitly opted in.
                var comparteDatosMedicos = perfil.PermitirCompartirDatosMedicos == true;

                var condiciones = comparteDatosMedicos
                    ? await _db.condicionUsuario
                        .AsNoTracking()
                        .Where(c => c.idUsuario == perfil.idUser && !c.Eliminado)
                        .Include(c => c.Condicion)
                        .OrderByDescending(c => c.fechaCreado)
                        .Take(3)
                        .Select(c => new InfoClinicaVm
                        {
                            Nombre = c.Condicion.nombre ?? "Sin nombre",
                            Icono = c.Condicion.icono,
                            FechaInicio = c.fechaInicio,
                            EdadDiagnostico = null // computed below once we have perfil.FechaDeNacimiento
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                // Cargar últimos 3 síntomas (ya no se mostrarán en grid, solo para badge principal)
                var sintomas = comparteDatosMedicos
                    ? await _db.sintomasUsuario
                        .AsNoTracking()
                        .Where(s => s.idUsuario == perfil.idUser && !s.Eliminado)
                        .Include(s => s.Sintoma)
                        .OrderByDescending(s => s.fechaCreado)
                        .Take(3)
                        .Select(s => new InfoClinicaVm
                        {
                            Nombre = s.Sintoma.nombre ?? "Sin nombre",
                            Icono = s.Sintoma.icono,
                            FechaInicio = s.fechaInicio
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                // Cargar últimos 3 tratamientos
                var tratamientos = comparteDatosMedicos
                    ? await _db.tratamientoUsuario
                        .AsNoTracking()
                        .Where(t => t.idUsuario == perfil.idUser && !t.Eliminado)
                        .Include(t => t.Tratamiento)
                        .OrderByDescending(t => t.fechaCreado)
                        .Take(3)
                        .Select(t => new InfoClinicaVm
                        {
                            Nombre = t.Tratamiento.nombre ?? "Sin nombre",
                            Icono = t.Tratamiento.icono,
                            FechaInicio = t.fechaInicio
                        })
                        .ToListAsync()
                    : new List<InfoClinicaVm>();

                // ✅ NUEVO: Cargar TOP 10 Tracking de Síntomas
                var trackingSintomas = comparteDatosMedicos
                    ? await _db.TrackingSintomaUsuario
                        .AsNoTracking()
                        .Where(t => t.IdUsuario == perfil.idUser)
                        .Include(t => t.SintomaUsuario).ThenInclude(s => s.Sintoma)
                        .OrderByDescending(t => t.Fecha)
                        .Take(10)
                        .Select(t => new TrackingSintomaVm
                        {
                            SintomaNombre = t.SintomaUsuario.Sintoma.nombre ?? "Sin nombre",
                            Estado = t.Estado,
                            Fecha = t.Fecha
                        })
                        .ToListAsync()
                    : new List<TrackingSintomaVm>();

                // Cargar TOP 10 estados de ánimo
                var estadosAnimo = comparteDatosMedicos
                    ? await _db.EstadoAnimoUsuario
                        .AsNoTracking()
                        .Where(e => e.IdUsuario == perfil.idUser && !e.Eliminado)
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

                PerfilPublico = new PerfilPublicoVm
                {
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
                    TrackingSintomas = trackingSintomas // ✅ NUEVO
                };

                // Compute EdadDiagnostico for condiciones using perfil.FechaDeNacimiento
                if (perfil.FechaDeNacimiento.HasValue)
                {
                    foreach (var cond in PerfilPublico.Condiciones)
                    {
                        if (cond.FechaInicio.HasValue)
                        {
                            var age = perfil.FechaDeNacimiento.Value;
                            var diff = cond.FechaInicio.Value - age;
                            var years = (int)Math.Floor(diff.TotalDays / 365.25);
                            cond.EdadDiagnostico = years >= 0 && years < 120 ? years : (int?)null;
                        }
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
            if (!fechaNacimiento.HasValue || fechaNacimiento.Value > DateTime.Now)
                return null;

            var edad = DateTime.Now.Year - fechaNacimiento.Value.Year;
            if (DateTime.Now < fechaNacimiento.Value.AddYears(edad))
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

        // Información clínica
        public List<InfoClinicaVm> Condiciones { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Sintomas { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Tratamientos { get; set; } = new List<InfoClinicaVm>();
        public List<EstadoAnimoVm> EstadosAnimo { get; set; } = new List<EstadoAnimoVm>();
        public List<TrackingSintomaVm> TrackingSintomas { get; set; } = new List<TrackingSintomaVm>(); // ✅ NUEVO
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
}