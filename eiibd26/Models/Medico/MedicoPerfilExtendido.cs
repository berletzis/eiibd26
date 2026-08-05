using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio;
using eiibd26.Models.Directorio.Enums;

namespace eiibd26.Models.Medico;

public class MedicoPerfilExtendido
{
    public int Id { get; set; }
    public int? MedicoId { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(100)] public string? Slug { get; set; }
    [MaxLength(500)] public string? Foto { get; set; }
    [MaxLength(2000)] public string? Biografia { get; set; }
    [MaxLength(1000)] public string? Hospitales { get; set; }
    [MaxLength(500)] public string? HorariosAtencion { get; set; }
    [MaxLength(300)] public string? SitioWeb { get; set; }
    [MaxLength(50)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Instagram { get; set; }
    [MaxLength(150)] public string? LinkedIn { get; set; }
    [MaxLength(150)] public string? Estado { get; set; }
    [MaxLength(150)] public string? Ciudad { get; set; }
    [MaxLength(10)]  public string? PaisCodigo { get; set; }
    [MaxLength(50)]  public string? Latitud { get; set; }
    [MaxLength(50)]  public string? Longitud { get; set; }

    /// <summary>Tipo de profesional — dato estructurado que GUÍA qué se le sugiere validar primero.
    /// Vive en el perfil POR-USUARIO (no en la ficha del directorio) para que cualquier profesional
    /// pueda declararlo desde su perfil, tenga o no ficha vinculada. Null = "general" → TOP clínico.
    /// NO es un permiso: validar sigue dependiendo del rol "Medico".</summary>
    [Display(Name = "Tipo de profesional")]
    public TipoProfesional? TipoProfesional { get; set; }

    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificado { get; set; } = DateTime.UtcNow;
    public virtual MedicoDirectorio? Medico { get; set; }
    public virtual ApplicationUser? Usuario { get; set; }
}
