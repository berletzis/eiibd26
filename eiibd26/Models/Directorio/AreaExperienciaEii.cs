using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("AreaExperienciaEii")]
public class AreaExperienciaEii
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<MedicoExperienciaEii> MedicosExperiencia { get; set; } = new List<MedicoExperienciaEii>();
}
