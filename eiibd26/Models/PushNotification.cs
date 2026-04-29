using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class PushNotification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Body { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Icon { get; set; }

        [MaxLength(500)]
        public string? Url { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ScheduledFor { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public bool IsSent { get; set; } = false;

        public string? TargetUserIds { get; set; } // JSON array or "all"

        public int TotalSent { get; set; } = 0;

        public int TotalFailed { get; set; } = 0;

        /// <summary>Tipo estructurado de notificación: Mood, Respuesta, Recordatorio, General</summary>
        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = "General";

        // Navigation - Especificar FK explícitamente
        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser? Creator { get; set; }
    }
}
