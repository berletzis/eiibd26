using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class EstadoAnimoUsuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid IdUsuario { get; set; }

        [Required]
        [StringLength(10)]
        public string EstadoMood { get; set; } // MuyBien, Bien, Neutral, Mal, MuyMal

        public string? Texto { get; set; }

        [Required]
        public DateTime FechaRegistro { get; set; }


        // Soft-delete flag
        public bool Eliminado { get; set; } = false;

        // Relaciones opcionales a contexto clínico del usuario
        public int? IdCondicionUsuario { get; set; }
        public int? IdSintomaUsuario { get; set; }
        public int? IdTratamientoUsuario { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public virtual ApplicationUser Usuario { get; set; }

        [ForeignKey(nameof(IdCondicionUsuario))]
        public virtual condicionUsuario CondicionUsuario { get; set; }

        [ForeignKey(nameof(IdSintomaUsuario))]
        public virtual sintomasUsuario SintomaUsuario { get; set; }

        [ForeignKey(nameof(IdTratamientoUsuario))]
        public virtual tratamientoUsuario TratamientoUsuario { get; set; }
    }
}