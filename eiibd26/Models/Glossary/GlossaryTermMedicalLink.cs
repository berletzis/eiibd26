using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Relación OPCIONAL entre términos del glosario y entidades médicas.
    /// Actúa como ADAPTER para lectura, NO como FK de ownership.
    /// </summary>
    [Table("GlossaryTermMedicalLink")]
    public class GlossaryTermMedicalLink
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del término en el glosario
        /// </summary>
        [Required]
        public int GlossaryTermId { get; set; }

        /// <summary>
        /// ID del síntoma médico (si aplica)
        /// </summary>
        public int? SintomaId { get; set; }

        /// <summary>
        /// ID del tratamiento médico (si aplica)
        /// </summary>
        public int? TratamientoId { get; set; }

        // ⭐ NAVEGACIÓN (sin FK constraint por arquitectura desacoplada)
        [ForeignKey("GlossaryTermId")]
        public virtual GlossaryTerm? GlossaryTerm { get; set; }

        // ⚠️ NO configurar FK a sintomas/tratamientos para mantener bajo acoplamiento
        // La relación se maneja a nivel de servicio/adapter
    }
}
