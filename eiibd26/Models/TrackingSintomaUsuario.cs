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

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser Usuario { get; set; }

        [ForeignKey(nameof(IdSintomaUsuario))]
        public virtual sintomasUsuario SintomaUsuario { get; set; }
    }
}