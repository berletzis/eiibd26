using eiibd26.Services.Validacion;

namespace eiibd26.Services.Glossary.DTOs
{
    public record GlossaryTermSummaryDto
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = "";
        public string Slug { get; init; } = "";
        public string? ShortDescription { get; init; }
        public DateTime? LastHumanUpdateDate { get; init; }
        public int Views { get; init; }
        public bool IsValidated { get; init; }
        public bool IsReviewedByHuman { get; init; }
        public bool HasRelationBadge { get; init; }
        /// <summary>Cantidad de usuarios que tienen este síntoma/tratamiento en su perfil</summary>
        public int UserRelationCount { get; init; }
        // Counts for relation levels
        public int RelationDirectCount { get; init; }
        public int RelationIndirectCount { get; init; }
        public int RelationSecondaryCount { get; init; }
        public int RelationTotalCount => RelationDirectCount + RelationIndirectCount + RelationSecondaryCount;

        public List<ValidacionPublicaDto> Validadores { get; set; } = new();
    }
}
