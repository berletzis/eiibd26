using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Glossary;

/// <summary>
/// Represents a user rating (Like/Dislike) on a glossary term.
/// Follows the same pattern as ArticleRating for article content.
/// </summary>
[Table("GlossaryTermRatings")]
public class GlossaryTermRating
{
    [Key]
    public int Id { get; set; }

    /// <summary>ID del término calificado.</summary>
    [Required]
    public int TermId { get; set; }

    /// <summary>Tipo de calificación: Like (1) o Dislike (0).</summary>
    [Required]
    public RatingType RatingType { get; set; }

    /// <summary>ID del usuario que calificó (null si es anónimo).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Dirección IP del usuario (útil para usuarios anónimos).</summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(TermId))]
    public virtual GlossaryTerm? Term { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
