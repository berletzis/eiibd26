using System.ComponentModel.DataAnnotations;

namespace eiibd26.Models.Medico;

public class MedicoBadge
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Nombre { get; set; } = string.Empty;
    [Required, MaxLength(500)] public string Descripcion { get; set; } = string.Empty;
    [Required, MaxLength(300)] public string ComoObtenerlo { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Icono { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public virtual ICollection<MedicoPerfilBadge> PerfilesBadges { get; set; } = new List<MedicoPerfilBadge>();
}
