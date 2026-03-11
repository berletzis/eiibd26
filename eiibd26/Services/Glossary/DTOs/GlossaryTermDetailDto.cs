using eiibd26.Models.Glossary;

namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// DTO para mostrar detalle completo de un término del glosario
    /// </summary>
    public class GlossaryTermDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Slug { get; set; } = "";
        public GlossaryTermType TipoTermino { get; set; }

        /// <summary>
        /// Id del síntoma médico vinculado (si aplica)
        /// </summary>
        public int? SintomaId { get; set; }

        /// <summary>
        /// Id del tratamiento médico vinculado (si aplica)
        /// </summary>
        public int? TratamientoId { get; set; }

        /// <summary>
        /// Definición médica oficial (leída desde sintomas/tratamientos)
        /// </summary>
        public MedicalDefinitionDto? DefinicionMedica { get; set; }

        /// <summary>
        /// Artículos relacionados del CMS
        /// </summary>
        public List<RelatedContentDto> ArticulosRelacionados { get; set; } = new();

        /// <summary>
        /// Conteos de badges de confianza (IA + validaciones humanas)
        /// </summary>
        public GlossaryValidationCountsDto ValidationCounts { get; set; } = new();

        /// <summary>
        /// Experiencias recientes de la comunidad (READ-ONLY de EstadoAnimoUsuario)
        /// </summary>
        public List<CommunityExperienceDto> ExperienciasComunidad { get; set; } = new();
    }
}
