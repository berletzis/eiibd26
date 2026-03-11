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

        // ===== IA + VALIDACIÓN MÉDICA =====

        /// <summary>
        /// Nivel de relación con EII sugerido por NINA (IA)
        /// </summary>
        public MedicalRelationType? MedicalRelationSuggestedId { get; set; }

        /// <summary>
        /// Nivel de relación con EII confirmado manualmente (admin)
        /// </summary>
        public MedicalRelationType? MedicalRelationTypeId { get; set; }

        /// <summary>
        /// Razonamiento clínico breve generado por la IA
        /// </summary>
        [MaxLength(1000)]
        public string? AiReasoning { get; set; }

        /// <summary>
        /// Indica si el contenido fue generado por NINA (IA)
        /// </summary>
        public bool CreatedByAI { get; set; } = true;

        // ⭐ RELACIÓN OPCIONAL CON DATOS MÉDICOS (a través de adapter)
        public virtual GlossaryTermMedicalLink? MedicalLink { get; set; }

        /// <summary>Validaciones humanas acumulativas</summary>
        public virtual ICollection<GlossaryValidation> Validaciones { get; set; } = new List<GlossaryValidation>();
    }
}
