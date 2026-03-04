using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class NotificationSubscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Endpoint { get; set; } = string.Empty;

        [MaxLength(500)]
        public string P256dh { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Auth { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? LastUsed { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation - Especificar FK explícitamente
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
