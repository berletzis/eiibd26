using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Directorio;

[Table("DirectorioMedicoConfirmacion")]
public class DirectorioMedicoConfirmacion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int MedicoId { get; set; }

    [Required]
    public Guid UsuarioId { get; set; }

    // Campo legado — se mantiene para compatibilidad
    public bool TieneExperienciaEII { get; set; } = false;

    // Áreas EII específicas reportadas
    public bool ExpCUCI { get; set; } = false;
    public bool ExpCrohn { get; set; } = false;
    public bool ExpPediatrico { get; set; } = false;
    public bool ExpOstomias { get; set; } = false;
    public bool ExpBiologicos { get; set; } = false;
    public bool ExpEmbarazoEII { get; set; } = false;
    public bool ExpManejoBrotes { get; set; } = false;
    public bool ExpSegundaOpinion { get; set; } = false;
    public bool ExpCirugia { get; set; } = false;
    public bool ExpSeguimientoProlongado { get; set; } = false;

    public DateTime FechaConfirmacion { get; set; } = DateTime.UtcNow;

    public bool Eliminado { get; set; } = false;

    [ForeignKey(nameof(UsuarioId))]
    public virtual ApplicationUser? Usuario { get; set; }

    // Propiedad computada: tiene al menos una exp. EII específica
    [NotMapped]
    public bool TieneAlgunaExpEII =>
        ExpCUCI || ExpCrohn || ExpPediatrico || ExpOstomias || ExpBiologicos ||
        ExpEmbarazoEII || ExpManejoBrotes || ExpSegundaOpinion || ExpCirugia || ExpSeguimientoProlongado;
}
