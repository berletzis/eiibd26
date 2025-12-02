namespace NINA_WorkerService.Models;

public class ArticleSummary
{
    public int ArticleSummaryId { get; set; }
    public int ArticleId { get; set; }
    public string SummaryText { get; set; } = null!;
    public string Language { get; set; } = "es";
    public string SummaryType { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    public string? ModelUsed { get; set; }
    public int? Version { get; set; }

    public Article Article { get; set; } = null!;
}