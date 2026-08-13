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
        /// Triage de limpieza del registro médico vinculado:
        /// null = no revisado · 1 = Válido · 2 = Basura · 3 = Dudoso.
        /// </summary>
        /// <remarks>
        /// Un término Dudoso sigue <c>Activo</c> (no se despublica por dudar), pero NO debe
        /// indexarse: es contenido cuya veracidad todavía no confirmó nadie.
        /// </remarks>
        public byte? TriageEstado { get; set; }

        /// <summary>
        /// Justificación del triage (por qué Válido/Basura/Dudoso), tal como la escribió NINA.
        /// SOLO se muestra a perfiles Administrador/Medico — es razonamiento interno de curaduría,
        /// no contenido para el paciente. Ver Termino.cshtml.
        /// </summary>
        public string? TriageMotivo { get; set; }

        /// <summary>
        /// Artículos relacionados del CMS
        /// </summary>
        public List<RelatedContentDto> ArticulosRelacionados { get; set; } = new();

        /// <summary>
        /// Preguntas relacionadas (top N) para este término
        /// </summary>
        public List<RelatedQuestionDto> PreguntasRelacionadas { get; set; } = new();

        /// <summary>
        /// Conteos de badges de confianza (IA + validaciones humanas)
        /// </summary>
        public GlossaryValidationCountsDto ValidationCounts { get; set; } = new();

        /// <summary>
        /// Experiencias recientes de la comunidad (READ-ONLY de EstadoAnimoUsuario)
        /// </summary>
        public List<CommunityExperienceDto> ExperienciasComunidad { get; set; } = new();

        /// <summary>
        /// Cantidad total de usuarios que tienen este síntoma/tratamiento en su perfil
        /// (tabla sintomasUsuario / tratamientoUsuario). Usado para mostrar "15 usuarios · mostrando 5".
        /// </summary>
        public int RelatedUsersCount { get; set; } = 0;
    }
}
