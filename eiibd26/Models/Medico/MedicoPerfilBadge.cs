using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

public class MedicoPerfilBadge
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public int BadgeId { get; set; }
    public DateTime FechaObtenido { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(50)] public string OtorgadoPor { get; set; } = "sistema";

    public bool EnRevision { get; set; }
    [MaxLength(300)] public string? RevisionMotivo { get; set; }
    [MaxLength(50)]  public string? RevisionPor    { get; set; }
    public DateTime? FechaRevision { get; set; }

    public virtual MedicoDirectorio? Medico { get; set; }
    public virtual MedicoBadge? Badge { get; set; }
}
