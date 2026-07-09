namespace NINA_WorkerService.Models;

public class ScrapedPage
{
    public int ScrapedPageId { get; set; }
    public int SourceSiteId { get; set; }
    public string Url { get; set; } = null!;
    public string? TitleRaw { get; set; }
    public string ContentRaw { get; set; } = null!;
    public string? ContentText { get; set; }
    public string Language { get; set; } = "es";
    public DateTime? PublishedAt { get; set; }
    public DateTime ScrapedAt { get; set; }
    public byte[]? HashContent { get; set; }
    public string Status { get; set; } = "OK";
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Firma de cobertura EII del externo (JSON { v, totalTokens, counts }), calculada en
    /// memoria durante el crawl con la biblioteca compartida eiibd26.Firma. NUNCA se guarda
    /// el texto ni la traducción — solo esta firma. NULL = aún no firmado.
    /// </summary>
    public string? Firma { get; set; }

    /// <summary>Momento del último cálculo de <see cref="Firma"/>. NULL = nunca firmado.</summary>
    public DateTime? FirmaCalculadaEn { get; set; }

    /// <summary>
    /// Embedding semántico denso (Voyage) del externo: JSON de un array de floats (1024 dims de
    /// voyage-4-large). Como la firma, se calcula en memoria durante el crawl con el texto ya
    /// extraído; NUNCA se guarda el texto. Multilingüe: el inglés se embebe SIN traducir.
    /// NULL = aún no embebido. Columna por SQL directo (sin migración).
    /// </summary>
    public string? Embedding { get; set; }

    /// <summary>Modelo que generó el <see cref="Embedding"/> (p. ej. "voyage-4-large").</summary>
    public string? EmbeddingModelo { get; set; }

    /// <summary>Momento del último cálculo de <see cref="Embedding"/>. NULL = nunca embebido.</summary>
    public DateTime? EmbeddingCalculadoEn { get; set; }

    public SourceSite SourceSite { get; set; } = null!;
    public Article? Article { get; set; }
}