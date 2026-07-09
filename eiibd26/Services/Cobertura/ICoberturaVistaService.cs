using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eiibd26.Services.Cobertura
{
    /// <summary>
    /// Lecturas de Fase 4 (vistas paciente + admin) sobre el Motor de Cobertura.
    /// SOLO LECTURA de CoberturaSimilitud / ScrapedPage / SourceSite / categorías.
    /// El filtro de "artículo real" es pertenecer al árbol de la categoría General (Sequence=1).
    /// </summary>
    public interface ICoberturaVistaService
    {
        /// <summary>¿El contenido es un artículo real (árbol General), no una página de sistema?</summary>
        Task<bool> EsArticuloAsync(int contenidoId, CancellationToken ct = default);

        /// <summary>
        /// Sitios externos con contenido similar al artículo dado (TipoPar=2, AId=artículo),
        /// score ≥ umbral paciente (0.60), top N por score. Vacío si no es artículo.
        /// </summary>
        Task<IReadOnlyList<ExternoSimilarDto>> ObtenerExternosSimilaresAsync(int contenidoId, CancellationToken ct = default);

        /// <summary>Grid admin: cada tema externo escaneado y su mejor match propio (artículo).</summary>
        Task<IReadOnlyList<CoberturaTemaDto>> ObtenerCoberturaTemasAsync(string? orden, CancellationToken ct = default);
    }

    /// <summary>Un sitio externo relacionado, para la vista paciente.</summary>
    public sealed class ExternoSimilarDto
    {
        public int ScrapedPageId { get; init; }
        public string Url { get; init; } = "";
        public string SitioNombre { get; init; } = "";
        public string Titulo { get; init; } = "";
        public double Score { get; init; }

        /// <summary>Etiqueta protagonista de la relación (según score).</summary>
        public string Etiqueta => Score >= 0.80 ? "Muy relacionado" : "Relacionado";
        /// <summary>% redondeado para mostrar de forma discreta.</summary>
        public int Porcentaje => (int)System.Math.Round(Score * 100);
    }

    /// <summary>Una fila del grid de cobertura admin (por tema externo).</summary>
    public sealed class CoberturaTemaDto
    {
        public int ScrapedPageId { get; init; }
        public string Url { get; init; } = "";
        public string SitioNombre { get; init; } = "";
        public string Titulo { get; init; } = "";
        public string Idioma { get; init; } = "";

        /// <summary>Mejor score contra un artículo propio (null = sin match guardado = hueco).</summary>
        public double? MejorScore { get; init; }
        public int? MejorArticuloId { get; init; }
        public string? MejorArticuloTitulo { get; init; }

        public int? Porcentaje => MejorScore == null ? (int?)null : (int)System.Math.Round(MejorScore.Value * 100);
        /// <summary>Cubierto (≥0.60) · Débil (0.50–0.60) · Hueco (sin match).</summary>
        public string Estado =>
            MejorScore == null ? "Hueco" : MejorScore >= 0.60 ? "Cubierto" : "Débil";
    }
}
