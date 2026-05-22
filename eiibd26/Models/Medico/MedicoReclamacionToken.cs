using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

public class MedicoReclamacionToken
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    [Required, MaxLength(200)] public string Token { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string EmailDestino { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpira { get; set; }
    public DateTime? FechaUsado { get; set; }
    public bool Activo { get; set; } = true;
    public virtual MedicoDirectorio? Medico { get; set; }
    public virtual ApplicationUser? Usuario { get; set; }
}
