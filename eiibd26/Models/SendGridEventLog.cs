using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Registra eventos recibidos del webhook de SendGrid.
    /// Los custom args (UserId, FaseLog, CampanaCodigo, TemplateId, EnvioTimestamp)
    /// se inyectan al enviar y SendGrid los devuelve en cada evento.
    ///
    /// Keys de custom args usadas al enviar (F2 las lee con estos nombres exactos):
    ///   "userId"     → UserId (Guid como string)
    ///   "campana"    → CampanaCodigo (ej. "reactivacion", "general")
    ///   "fase"       → FaseLog (int como string)
    ///   "templateId" → TemplateId (solo campaña general)
    ///   "envio_ts"   → EnvioTimestamp (ISO 8601 del momento del envío)
    /// </summary>
    [Table("SendGridEventLog")]
    public class SendGridEventLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        [MaxLength(200)]
        public string? SgMessageId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        // Custom args devueltos por SendGrid
        [MaxLength(36)]
        public string? UserId { get; set; }

        public int? FaseLog { get; set; }

        [MaxLength(50)]
        public string? CampanaCodigo { get; set; }

        [MaxLength(200)]
        public string? TemplateId { get; set; }

        [MaxLength(40)]
        public string? EnvioTimestamp { get; set; }

        // Datos específicos por tipo de evento
        [MaxLength(500)]
        public string? Url { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(50)]
        public string? BounceType { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        public string? RawJson { get; set; }
    }
}
