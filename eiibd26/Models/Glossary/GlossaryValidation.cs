using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eiibd26.Models.Glossary
{
    /// <summary>
    /// Registro de validación humana sobre un término del glosario.
    /// Acumulativas: nunca se eliminan; reflejan consenso progresivo.
    /// </summary>
    [Table("GlossaryValidation")]
    public class GlossaryValidation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GlossaryTermId { get; set; }

        /// <summary>ASP.NET Identity UserId (nvarchar 450)</summary>
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = "";

        [Required]
        public GlossaryValidationType ValidationType { get; set; }

        /// <summary>Solo aplica cuando ValidationType = RelationValidation</summary>
        public MedicalRelationType? MedicalRelationTypeId { get; set; }

        public bool Approved { get; set; }

        /// <summary>Comentario clínico opcional (máx 800 caracteres)</summary>
        [MaxLength(800)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("GlossaryTermId")]
        public virtual GlossaryTerm? GlossaryTerm { get; set; }
    }
}
