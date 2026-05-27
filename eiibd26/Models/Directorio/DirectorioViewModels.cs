using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Models.Directorio;

// ── LISTADO ──────────────────────────────────────────────────────────────

public class DirectorioIndexVm
{
    public List<MedicoCardVm> Medicos { get; set; } = new();
    public string? FiltroBusqueda { get; set; }
    public string? FiltroEstado { get; set; }
    public string? FiltroEspecialidad { get; set; }
    public int? FiltroAreaId { get; set; }
    public List<AreaExperienciaEii> AreasDisponibles { get; set; } = new();
    public List<string> EstadosDisponibles { get; set; } = new();
    public int TotalResultados { get; set; }
    public int PaginaActual { get; set; } = 1;
    public int TotalPaginas { get; set; }
}

public class MedicoCardVm
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
    public string? Subespecialidad { get; set; }
    public string? Estado { get; set; }
    public string? Ciudad { get; set; }
    public string? HospitalClinica { get; set; }
    public NivelConfianzaEnum NivelConfianza { get; set; }
    public string NivelConfianzaLabel => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "Identificado por pacientes",
        NivelConfianzaEnum.Confirmado   => "Confirmado por la comunidad",
        NivelConfianzaEnum.Reconocido   => "Reconocido en EII",
        NivelConfianzaEnum.Establecido  => "Experiencia establecida en EII",
        _                               => "Identificado por pacientes"
    };
    public string NivelConfianzaBadgeClass => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "badge-nivel-0",
        NivelConfianzaEnum.Confirmado   => "badge-nivel-1",
        NivelConfianzaEnum.Reconocido   => "badge-nivel-2",
        NivelConfianzaEnum.Establecido  => "badge-nivel-3",
        _                               => "badge-nivel-0"
    };
    public EstatusValidacionCedula EstatusValidacion { get; set; }
    public bool CedulaValidada => EstatusValidacion == EstatusValidacionCedula.Validado;
    public int TotalConfirmaciones { get; set; }
    public int TotalPacientesUnicos { get; set; }
    public List<string> AreasExperiencia { get; set; } = new();
    public HashSet<string> BadgesGanados { get; set; } = new();
}

// ── UBICACIONES COMBINADAS (médico + pacientes) ──────────────────────────

public class UbicacionMedicoVm
{
    public string Hospital { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Pais { get; set; } = "";
    public string Fuente { get; set; } = "";  // "medico" | "paciente"
    public int Reportes { get; set; } = 1;
}

// ── FICHA DETALLE ────────────────────────────────────────────────────────

public class MedicoDetalleVm
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? CedulaProfesional { get; set; }
    public string? Especialidad { get; set; }
    public string? Subespecialidad { get; set; }
    public string? Estado { get; set; }
    public string? Ciudad { get; set; }
    public string? MunicipioAlcaldia { get; set; }
    public string? HospitalClinica { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    // Campos del perfil extendido (ingresados por el médico)
    public string? Foto { get; set; }
    public string? Biografia { get; set; }
    public string? HorariosAtencion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Instagram { get; set; }
    public string? LinkedIn { get; set; }
    public NivelConfianzaEnum NivelConfianza { get; set; }
    public string NivelConfianzaLabel => NivelConfianza switch
    {
        NivelConfianzaEnum.Identificado => "Identificado por pacientes con EII",
        NivelConfianzaEnum.Confirmado   => "Confirmado por la comunidad EII",
        NivelConfianzaEnum.Reconocido   => "Reconocido con experiencia en EII",
        NivelConfianzaEnum.Establecido  => "Experiencia establecida y sostenida en EII",
        _                               => "Identificado por pacientes con EII"
    };
    public EstatusValidacionCedula EstatusValidacion { get; set; }
    public EstatusReclamacion EstatusReclamacion { get; set; }
    public bool PerfilReclamable => EstatusReclamacion == EstatusReclamacion.NoReclamado;
    public int TotalConfirmaciones { get; set; }
    public int TotalPacientesUnicos { get; set; }
    public List<AreaExperienciaVm> AreasExperiencia { get; set; } = new();
    public List<ConfirmacionAgregadaVm> ConfirmacionesAgregadas { get; set; } = new();
    public bool UsuarioYaConfirmo { get; set; }
    public List<int> TiposConfirmadosPorUsuario { get; set; } = new();
}

public class AreaExperienciaVm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ConfirmacionAgregadaVm
{
    public int TipoConfirmacionId { get; set; }
    public string NombreTipo { get; set; } = string.Empty;
    public int Total { get; set; }
}

// ── FORMULARIO PROPONER MÉDICO ───────────────────────────────────────────

public class ProponerMedicoVm
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MaxLength(300)]
    [Display(Name = "Nombre completo del médico")]
    public string NombreCompleto { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Cédula profesional (si la conoces)")]
    public string? CedulaProfesional { get; set; }

    [MaxLength(200)]
    [Display(Name = "Especialidad")]
    public string? Especialidad { get; set; }

    [MaxLength(200)]
    [Display(Name = "Subespecialidad")]
    public string? Subespecialidad { get; set; }

    [MaxLength(100)]
    [Display(Name = "País")]
    public string? NombrePais { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio")]
    [MaxLength(100)]
    [Display(Name = "Ciudad / Estado")]
    public string Estado { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Ciudad")]
    public string? Ciudad { get; set; }

    [MaxLength(100)]
    [Display(Name = "Municipio / Alcaldía")]
    public string? MunicipioAlcaldia { get; set; }

    [MaxLength(300)]
    [Display(Name = "Hospital o Clínica")]
    public string? HospitalClinica { get; set; }

    [Display(Name = "Latitud")]
    public decimal? Latitud { get; set; }

    [Display(Name = "Longitud")]
    public decimal? Longitud { get; set; }

    [Display(Name = "Áreas de experiencia EII que reportas")]
    public List<int> AreasSeleccionadas { get; set; } = new();

    public List<AreaExperienciaEii> AreasDisponibles { get; set; } = new();
}

// ── PROPUESTA PACIENTE (estado dinámico) ─────────────────────────────────

public class PropuestaPacienteVm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Especialidad { get; set; }
    public string? Estado { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public EstadoProfesionalDerivado EstadoDerivado { get; set; }

    public string EstadoLabel => EstadoDerivado switch
    {
        EstadoProfesionalDerivado.CedulaVerificada => "Cédula verificada",
        EstadoProfesionalDerivado.Publicado        => "Publicado en el directorio",
        EstadoProfesionalDerivado.Reclamado        => "Reclamado por el profesional",
        _                                          => "En revisión"
    };

    public string EstadoBadgeClass => EstadoDerivado switch
    {
        EstadoProfesionalDerivado.CedulaVerificada => "bg-success",
        EstadoProfesionalDerivado.Publicado        => "bg-primary",
        EstadoProfesionalDerivado.Reclamado        => "bg-info text-dark",
        _                                          => "bg-warning text-dark"
    };
}

// ── CONFIRMAR ATENCIÓN ───────────────────────────────────────────────────

public class ConfirmarAtencionVm
{
    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required(ErrorMessage = "Selecciona al menos un tipo de atención")]
    [Display(Name = "Tipo de atención recibida")]
    public int TipoConfirmacionId { get; set; }

    public List<TipoConfirmacion> TiposDisponibles { get; set; } = new();
    public string NombreMedico { get; set; } = string.Empty;
}
