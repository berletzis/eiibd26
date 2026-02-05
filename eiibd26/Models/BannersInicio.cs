using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    [Table("BannersInicio")]
    public class BannerInicio
    {
        [Key]
        public int Id { get; set; }

        [StringLength(250)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? Subtitle { get; set; }

        // Legacy single image (compat)
        [StringLength(500)]
        public string? ImagePath { get; set; }

        public bool IsImage { get; set; }
        public bool IsActive { get; set; }
        public bool VisibleToAuthenticated { get; set; }
        public bool VisibleToAnonymous { get; set; }
        public int SortOrder { get; set; }

        // Compatibilidad AgregadoPor (estado intermedio DB)
        [Column("AgregadoPor")]
        [StringLength(450)]
        public string? AgregadoPorString { get; set; }

        // GUID column (AgregadoPor_Guid) — si renuevas DB más tarde, elimina la string property
        [Column("AgregadoPor_Guid")]
        public Guid? AgregadoPor { get; set; }

        // --- NUEVOS CAMPOS (metadata y dos versiones de imagen) ---
        // Desktop
        [StringLength(500)]
        public string? ImagePathDesktop { get; set; }        // /uploads/banners/xxx.jpg (public path)
        [StringLength(250)]
        public string? ImageFileNameDesktop { get; set; }    // filename on disk
        [StringLength(100)]
        public string? ImageContentTypeDesktop { get; set; } // image/jpeg
        public long? ImageSizeDesktop { get; set; }          // bytes

        // Mobile
        [StringLength(500)]
        public string? ImagePathMobile { get; set; }
        [StringLength(250)]
        public string? ImageFileNameMobile { get; set; }
        [StringLength(100)]
        public string? ImageContentTypeMobile { get; set; }
        public long? ImageSizeMobile { get; set; }

        // Target URL when banner is clicked (same for both images)
        [StringLength(500)]
        public string? TargetUrl { get; set; }

        // Soft-delete flag
        public bool Borrado { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}