using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("TipoConfirmacion")]
public class TipoConfirmacion
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; } = true;

    public virtual ICollection<ConfirmacionComunitaria> Confirmaciones { get; set; } = new List<ConfirmacionComunitaria>();
}
