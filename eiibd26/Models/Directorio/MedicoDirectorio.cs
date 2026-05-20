using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Models.Directorio;

[Table("MedicosDirectorio")]
public class MedicoDirectorio
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(300)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Cédula profesional")]
    public string? CedulaProfesional { get; set; }

    [MaxLength(200)]
    [Display(Name = "Especialidad")]
    public string? Especialidad { get; set; }

    [MaxLength(200)]
    [Display(Name = "Subespecialidad")]
    public string? Subespecialidad { get; set; }

    [MaxLength(100)]
    [Display(Name = "Estado")]
    public string? Estado { get; set; }

    [MaxLength(100)]
    [Display(Name = "Ciudad")]
    public string? Ciudad { get; set; }

    [MaxLength(100)]
    [Display(Name = "Municipio / Alcaldía")]
    public string? MunicipioAlcaldia { get; set; }

    [MaxLength(300)]
    [Display(Name = "Hospital o Clínica")]
    public string? HospitalClinica { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    [Display(Name = "Latitud")]
    public decimal? Latitud { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    [Display(Name = "Longitud")]
    public decimal? Longitud { get; set; }

    [Display(Name = "Estatus de validación de cédula")]
    public EstatusValidacionCedula EstatusValidacion { get; set; } = EstatusValidacionCedula.PendienteValidacion;

    [Display(Name = "Nivel de confianza comunitaria")]
    public NivelConfianzaEnum NivelConfianza { get; set; } = NivelConfianzaEnum.Identificado;

    [Display(Name = "Estatus de reclamación")]
    public EstatusReclamacion EstatusReclamacion { get; set; } = EstatusReclamacion.NoReclamado;

    public Guid? AspNetUserId { get; set; }

    public DateTimeOffset? FechaReclamacion { get; set; }

    [Display(Name = "Visible públicamente")]
    public bool VisiblePublicamente { get; set; } = true;

    public bool Activo { get; set; } = true;

    public bool Eliminado { get; set; } = false;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FechaModificacion { get; set; }

    public Guid? PropuestoPorUsuarioId { get; set; }

    [ForeignKey(nameof(AspNetUserId))]
    public virtual ApplicationUser? UsuarioVinculado { get; set; }

    public virtual ICollection<MedicoExperienciaEii> AreasExperiencia { get; set; } = new List<MedicoExperienciaEii>();

    public virtual ICollection<ConfirmacionComunitaria> Confirmaciones { get; set; } = new List<ConfirmacionComunitaria>();
}
