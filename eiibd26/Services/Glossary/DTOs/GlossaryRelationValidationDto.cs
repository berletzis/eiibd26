using eiibd26.Models.Glossary;

namespace eiibd26.Services.Glossary.DTOs
{
    /// <summary>
    /// Una validación de RELACIÓN (nivel Directa/Indirecta/Secundaria) hecha por un
    /// profesional sobre un término del glosario, con los datos del término ya resueltos
    /// para poder listarla y enlazarla sin volver a consultar.
    /// </summary>
    public record GlossaryRelationValidationDto
    {
        public int Id { get; init; }
        public int GlossaryTermId { get; init; }
        public string TerminoNombre { get; init; } = "";
        public string TerminoSlug { get; init; } = "";
        public GlossaryTermType TipoTermino { get; init; }

        /// <summary>Nivel de relación que declaró el profesional (Directa/Indirecta/Secundaria).</summary>
        public MedicalRelationType? NivelRelacion { get; init; }

        /// <summary>False = registrada pero aún no cuenta para badges ni conteos públicos.</summary>
        public bool Aprobada { get; init; }

        public string? Comentario { get; init; }
        public DateTime CreadoEn { get; init; }

        /// <summary>Ruta pública del término — misma que arma ValidacionContenidoService.</summary>
        public string TerminoUrl => $"/Termino/{TerminoSlug}";

        public string NivelTexto => NivelRelacion switch
        {
            MedicalRelationType.Directa    => "Directa",
            MedicalRelationType.Indirecta  => "Indirecta",
            MedicalRelationType.Secundaria => "Secundaria",
            _                              => "Sin nivel"
        };

        public string TipoTerminoTexto => TipoTermino switch
        {
            GlossaryTermType.Sintoma            => "Síntoma",
            GlossaryTermType.Tratamiento        => "Tratamiento",
            GlossaryTermType.ConceptoGeneralEII => "Concepto EII",
            _                                   => ""
        };
    }
}
