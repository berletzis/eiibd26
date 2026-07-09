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
        /// Externos similares al artículo dado desde el MOTOR DE EMBEDDINGS
        /// (CoberturaSimilitudEmbedding, TipoPar=2, AId=artículo), paginado para "Ver más".
        /// <paramref name="tab"/> = <c>"similares"</c> (Score ≥ 0.78, subtema) o <c>"area"</c>
        /// (0.55 ≤ Score &lt; 0.78, área). Ordenado desc. Vacío si no es artículo.
        /// </summary>
        Task<SimilaresPagina> ObtenerSimilaresAsync(int contenidoId, string tab, int offset, int take, CancellationToken ct = default);

        /// <summary>
        /// "Similares de EIIBD": artículos PROPIOS semánticamente parecidos al dado
        /// (CoberturaSimilitudEmbedding TipoPar=1), top por Score, solo del árbol General.
        /// Reemplaza el "Recomendados" por categoría. Vacío si el dado no es artículo.
        /// </summary>
        Task<IReadOnlyList<SimilarPropioDto>> ObtenerSimilaresPropiosAsync(int contenidoId, int take, CancellationToken ct = default);

        /// <summary>Grid admin: cada tema externo escaneado y su mejor match propio (artículo).</summary>
        Task<IReadOnlyList<CoberturaTemaDto>> ObtenerCoberturaTemasAsync(string? orden, CancellationToken ct = default);
    }

    /// <summary>Un bloque paginado de externos similares + si quedan más (para "Ver más").</summary>
    public sealed class SimilaresPagina
    {
        public IReadOnlyList<ExternoSimilarDto> Items { get; init; } = System.Array.Empty<ExternoSimilarDto>();
        public bool HasMore { get; init; }
        public int NextOffset { get; init; }
    }

    /// <summary>Referencia a un artículo propio similar (por embeddings): Id + score.</summary>
    public sealed class SimilarPropioDto
    {
        public int Id { get; init; }
        public double Score { get; init; }
        public int Porcentaje => (int)System.Math.Round(Score * 100);
    }

    /// <summary>Un sitio externo relacionado, para la vista paciente.</summary>
    public sealed class ExternoSimilarDto
    {
        public int ScrapedPageId { get; init; }
        public string Url { get; init; } = "";
        public string SitioNombre { get; init; } = "";
        public string Titulo { get; init; } = "";
        public double Score { get; init; }

        /// <summary>% de coincidencia redondeado (única señal de score; el tab comunica el rol).</summary>
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
