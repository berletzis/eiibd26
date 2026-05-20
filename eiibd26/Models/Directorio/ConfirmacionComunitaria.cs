using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("ConfirmacionComunitaria")]
public class ConfirmacionComunitaria
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicoDirectorioId { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }

    [Required]
    public int TipoConfirmacionId { get; set; }

    public bool Eliminado { get; set; } = false;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(MedicoDirectorioId))]
    public virtual MedicoDirectorio MedicoDirectorio { get; set; } = null!;

    [ForeignKey(nameof(UsuarioId))]
    public virtual ApplicationUser Usuario { get; set; } = null!;

    [ForeignKey(nameof(TipoConfirmacionId))]
    public virtual TipoConfirmacion TipoConfirmacion { get; set; } = null!;
}
