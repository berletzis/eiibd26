using eiibd26.Models.Glossary;

namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// Conteos para el sistema de badges de confianza de un término.
    /// </summary>
    public class GlossaryValidationCountsDto
    {
        /// <summary>El contenido fue generado por NINA (IA)</summary>
        public bool CreatedByAI { get; set; }

        /// <summary>Cantidad de validaciones de descripción aprobadas</summary>
        public int MeaningValidationCount { get; set; }

        /// <summary>Nivel de relación sugerido por la IA</summary>
        public MedicalRelationType? RelationSuggested { get; set; }

        /// <summary>Razonamiento clínico generado por NINA al sugerir el nivel</summary>
        public string? AiReasoning { get; set; }

        /// <summary>Distribución de votos por nivel de relación médica</summary>
        public List<RelationConsensusItemDto> RelationConsensus { get; set; } = new();

        /// <summary>Comentarios de validadores de descripción (ValidationType = MeaningValidation)</summary>
        public List<string> MeaningComments { get; set; } = new();

        /// <summary>Comentarios clínicos de médicos con comentario no vacío</summary>
        public List<ValidationCommentDto> ComentariosMedicos { get; set; } = new();
    }

    /// <summary>Votos y comentarios para un nivel específico de relación médica</summary>
    public class RelationConsensusItemDto
    {
        public MedicalRelationType RelationType { get; set; }
        public int Count { get; set; }

        /// <summary>True cuando este nivel tiene el mayor número de votos</summary>
        public bool IsTopConsensus { get; set; }

        /// <summary>Comentarios clínicos de los validadores humanos para este nivel</summary>
        public List<string> Comments { get; set; } = new();
    }

    /// <summary>Comentario clínico de un médico sobre un término</summary>
    public class ValidationCommentDto
    {
        public string UserDisplay { get; set; } = "";
        public GlossaryValidationType ValidationType { get; set; }
        public MedicalRelationType? RelationType { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
