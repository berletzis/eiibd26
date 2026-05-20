using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("MedicoExperienciaEii")]
public class MedicoExperienciaEii
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required]
    public int AreaExperienciaEiiId { get; set; }

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public bool Eliminado { get; set; } = false;

    [ForeignKey(nameof(MedicoDirectorioId))]
    public virtual MedicoDirectorio MedicoDirectorio { get; set; } = null!;

    [ForeignKey(nameof(AreaExperienciaEiiId))]
    public virtual AreaExperienciaEii AreaExperienciaEii { get; set; } = null!;
}
