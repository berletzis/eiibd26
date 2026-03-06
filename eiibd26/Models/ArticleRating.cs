using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models;

/// <summary>
/// Representa una calificación (Like/Dislike) de un usuario sobre un artículo
/// </summary>
public class ArticleRating
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del artículo calificado
    /// </summary>
    [Required]
    public int ArticleId { get; set; }

    /// <summary>
    /// Tipo de calificación: Like (1) o Dislike (0)
    /// </summary>
    [Required]
    public RatingType RatingType { get; set; }

    /// <summary>
    /// ID del usuario que calificó (null si es anónimo)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Dirección IP del usuario (útil para usuarios anónimos)
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Fecha de creación de la calificación
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización (si el usuario cambió su voto)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ArticleId))]
    public virtual Contenido? Article { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
