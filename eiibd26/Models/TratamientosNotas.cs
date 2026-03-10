using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    public class TratamientosNotas
    {
        [Key]
        public int id { get; set; }

        [ForeignKey("Tratamiento")]
        public int TratamientoId { get; set; }

        [ForeignKey("Usuario")]
        public Guid? UsuarioId { get; set; }

        [Required]
        public string Nota { get; set; }

        [Display(Name = "Nota de IA")]
        public bool EsNotaIA { get; set; } = false;

        public DateTime FechaCreado { get; set; } = DateTime.UtcNow;
        public DateTime FechaModificado { get; set; } = DateTime.UtcNow;
        public bool Eliminado { get; set; } = false;

        // ===== NAVIGATION PROPERTIES =====
        public virtual tratamientos Tratamiento { get; set; }
        public virtual ApplicationUser Usuario { get; set; }
    }
}
