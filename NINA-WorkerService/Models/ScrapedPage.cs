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

    public SourceSite SourceSite { get; set; } = null!;
    public Article? Article { get; set; }
}