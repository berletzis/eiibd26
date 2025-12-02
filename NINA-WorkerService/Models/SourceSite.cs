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

    public ICollection<ScrapedPage> ScrapedPages { get; set; } = new List<ScrapedPage>();
}