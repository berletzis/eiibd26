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

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Slug vacío en solicitud de perfil público");
                return NotFound();
            }

            try
            {
                var perfil = await _db.Perfil
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.slug == slug && !string.IsNullOrWhiteSpace(p.Nombre));

                if (perfil == null)
                {
                    _logger.LogInformation("Perfil no encontrado para slug: {Slug}", slug);
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
                var condiciones = await _db.condicionUsuario
                    .AsNoTracking()
                    .Where(c => c.idUsuario == perfil.idUser && !c.Eliminado)
                    .Include(c => c.Condicion)
                    .OrderByDescending(c => c.fechaCreado)
                    .Take(3)
                    .Select(c => new InfoClinicaVm
                    {
                        Nombre = c.Condicion.nombre ?? "Sin nombre",
                        Icono = c.Condicion.icono,
                        FechaInicio = c.fechaInicio
                    })
                    .ToListAsync();

                // Cargar últimos 3 síntomas
                var sintomas = await _db.sintomasUsuario
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
                    .ToListAsync();

                // Cargar últimos 3 tratamientos
                var tratamientos = await _db.tratamientoUsuario
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
                    .ToListAsync();

                // Cargar últimos 3 estados de ánimo
                var estadosAnimo = await _db.EstadoAnimoUsuario
                    .AsNoTracking()
                    .Where(e => e.IdUsuario == perfil.idUser && !e.Eliminado)
                    .OrderByDescending(e => e.FechaRegistro)
                    .Take(3)
                    .Select(e => new EstadoAnimoVm
                    {
                        Estado = e.EstadoMood,
                        Texto = e.Texto,
                        FechaRegistro = e.FechaRegistro
                    })
                    .ToListAsync();

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
                    UbicacionTexto = ObtenerUbicacion(perfil),
                    MostrarUbicacion = perfil.PermitirMostrarPais ?? false,
                    FechaRegistro = fechaRegistro,
                    Condiciones = condiciones,
                    Sintomas = sintomas,
                    Tratamientos = tratamientos,
                    EstadosAnimo = estadosAnimo
                };

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

        // Información clínica
        public List<InfoClinicaVm> Condiciones { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Sintomas { get; set; } = new List<InfoClinicaVm>();
        public List<InfoClinicaVm> Tratamientos { get; set; } = new List<InfoClinicaVm>();
        public List<EstadoAnimoVm> EstadosAnimo { get; set; } = new List<EstadoAnimoVm>();
    }

    public class InfoClinicaVm
    {
        public string Nombre { get; set; }
        public string Icono { get; set; }
        public DateTime? FechaInicio { get; set; }
    }

    public class EstadoAnimoVm
    {
        public string Estado { get; set; }
        public string Texto { get; set; }
        public DateTime FechaRegistro { get; set; }

        public string ImagenEstado => Estado switch
        {
            "MuyBien" => "/img/muybien.svg",
            "Bien" => "/img/bien.svg",
            "Neutral" => "/img/neutral.svg",
            "Mal" => "/img/mal.svg",
            "MuyMal" => "/img/muymal.svg",
            _ => "/img/neutral.svg"
        };

        public string ColorEstado => Estado switch
        {
            "MuyBien" => "#38D6C1",    // Color del SVG muybien.svg
            "Bien" => "#B3F1E9",       // Color del SVG bien.svg
            "Neutral" => "#FEE019",    // Color del SVG neutral.svg
            "Mal" => "#D8B4F8",        // Color del SVG mal.svg
            "MuyMal" => "#9B5DE5",     // Color del SVG muymal.svg
            _ => "#9ca3af"
        };

        public string TextoEstado => Estado switch
        {
            "MuyBien" => "Muy bien",
            "Bien" => "Bien",
            "Neutral" => "Neutral",
            "Mal" => "Mal",
            "MuyMal" => "Muy mal",
            _ => "Desconocido"
        };
    }
}