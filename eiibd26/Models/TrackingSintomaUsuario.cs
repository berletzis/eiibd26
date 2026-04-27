using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class TrackingSintomaUsuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid IdUsuario { get; set; }

        [Required]
        public int IdSintomaUsuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(16)]
        public string Estado { get; set; } // "Ninguno", "Leve", "Moderado", "Severo"

        /// <summary>Nivel de dolor percibido en el momento del registro (0 = sin dolor, 10 = máximo).</summary>
        public int? Dolor { get; set; }

        /// <summary>Indica si el usuario reportó sangrado en este registro. Null si no fue informado.</summary>
        public bool? TieneSangrado { get; set; }

        /// <summary>Frecuencia del síntoma en el momento del registro. Null si no fue informado.</summary>
        public int? FrecuenciaId { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser Usuario { get; set; }

        [ForeignKey(nameof(IdSintomaUsuario))]
        public virtual sintomasUsuario SintomaUsuario { get; set; }

        [ForeignKey(nameof(FrecuenciaId))]
        public virtual FrecuenciaSintomaCatalog? Frecuencia { get; set; }
    }
}