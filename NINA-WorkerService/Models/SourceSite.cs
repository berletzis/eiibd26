namespace NINA_WorkerService.Models;

public class SourceSite
{
    public int SourceSiteId { get; set; }
    public string Name { get; set; } = null!;
    public string BaseUrl { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Metadatos de fuente (coinciden con SQL/alter-sourcesite-metadatos.sql).
    // Cada Article los hereda por join transitivo ScrapedPage -> SourceSite (no se duplican en Article).
    public string? Idioma { get; set; }      // NVARCHAR(10)  — ej. "es", "en"
    public string? Pais { get; set; }        // NVARCHAR(10)  — ej. "MX", "US"
    public string? Categoria { get; set; }   // NVARCHAR(100) — ej. "general"
    public string? UrlPublica { get; set; }  // NVARCHAR(500) — URL pública del sitio

    public ICollection<ScrapedPage> ScrapedPages { get; set; } = new List<ScrapedPage>();
}