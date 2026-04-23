using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models
{
    /// <summary>
    /// Registra el historial de envíos de campaña por usuario y fase.
    /// La unicidad de (UserId, Fase) con Exito=true se garantiza a nivel de lógica de negocio
    /// y por el índice único IX_EmailCampanaLog_UserId_Fase_Exito en la tabla.
    /// </summary>
    public class EmailCampanaLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        /// <summary>Fase de la campaña: 1 = Reactivación, 2 = Activación, 3 = Recordatorio</summary>
        [Required]
        public int Fase { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateId { get; set; }

        [Required]
        public DateTime FechaEnvio { get; set; }

        public bool Exito { get; set; }

        [MaxLength(500)]
        public string Error { get; set; }
    }
}
