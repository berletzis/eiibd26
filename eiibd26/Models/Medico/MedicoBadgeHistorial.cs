using System.ComponentModel.DataAnnotations;
using eiibd26.Models.Directorio;

namespace eiibd26.Models.Medico;

/// <summary>
/// Registro inmutable de cada evento de otorgamiento o revocación de un badge.
/// Solo se insertan filas, nunca se modifican (append-only).
/// </summary>
public class MedicoBadgeHistorial
{
    public int Id { get; set; }
    public int MedicoId { get; set; }
    public int BadgeId { get; set; }

    /// <summary>"otorgado" | "revocado"</summary>
    [Required, MaxLength(20)]
    public string Evento { get; set; } = string.Empty;

    /// <summary>Identificador del actor: "sistema", "admin", nombre de usuario, etc.</summary>
    [MaxLength(200)]
    public string Actor { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Motivo { get; set; }

    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

    public virtual MedicoDirectorio? Medico { get; set; }
    public virtual MedicoBadge? Badge { get; set; }
}
