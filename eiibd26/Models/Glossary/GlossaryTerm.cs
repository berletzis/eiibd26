using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Término del glosario médico (índice navegable, NO almacena descripciones)
    /// </summary>
    [Table("GlossaryTerm")]
    public class GlossaryTerm
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Nombre del término (ej: "Fatiga", "Prednisona")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = "";

        /// <summary>
        /// Slug para URL (ej: "fatiga", "prednisona")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Slug { get; set; } = "";

        /// <summary>
        /// Tipo de término (1=Sintoma, 2=Tratamiento)
        /// </summary>
        [Required]
        public GlossaryTermType TipoTermino { get; set; }

        /// <summary>
        /// Indica si el término está activo y visible
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTime? FechaActualizacion { get; set; }

        // ⭐ RELACIÓN OPCIONAL CON DATOS MÉDICOS (a través de adapter)
        public virtual GlossaryTermMedicalLink? MedicalLink { get; set; }
    }
}
