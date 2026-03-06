using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Feedback de usuarios sobre respuestas generadas por IA (NINA)
    /// Permite evaluar la utilidad de las respuestas de IA
    /// </summary>
    [Table("RespuestaAIFeedback")]
    public class RespuestaAIFeedback
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// ID de la respuesta de IA sobre la que se da feedback
        /// </summary>
        [Required]
        public Guid RespuestaId { get; set; }

        /// <summary>
        /// ID del usuario que da el feedback
        /// </summary>
        [Required]
        public Guid UsuarioId { get; set; }

        /// <summary>
        /// Indica si la respuesta fue útil
        /// true = 👍 (Like), false = 👎 (Dislike)
        /// </summary>
        [Required]
        public bool EsUtil { get; set; }

        /// <summary>
        /// Comentario opcional del usuario explicando por qué (máx 500 caracteres)
        /// Especialmente útil para feedback negativo
        /// </summary>
        [MaxLength(500)]
        public string? Comentario { get; set; }

        /// <summary>
        /// Fecha en que se dio el feedback
        /// </summary>
        [Required]
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        // ===== NAVIGATION PROPERTIES =====

        [ForeignKey(nameof(RespuestaId))]
        public virtual Respuesta Respuesta { get; set; } = null!;

        [ForeignKey(nameof(UsuarioId))]
        public virtual ApplicationUser Usuario { get; set; } = null!;
    }
}
