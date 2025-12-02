namespace NINA_WorkerService.Models;

public class Article
{
    public int ArticleId { get; set; }
    public int? ScrapedPageId { get; set; }
    public string NormalizedTitle { get; set; } = null!;
    public string NormalizedContent { get; set; } = null!;
    public string Language { get; set; } = "es";
    public string? MainTopic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    public ScrapedPage? ScrapedPage { get; set; }

    public ICollection<ArticleTranslation> Translations { get; set; } = new List<ArticleTranslation>();
    public ICollection<ArticleSummary> Summaries { get; set; } = new List<ArticleSummary>();
    public ICollection<ArticleBoost> Boosts { get; set; } = new List<ArticleBoost>();

    public ICollection<ArticleSimilarity> SimilaritiesAs1 { get; set; } = new List<ArticleSimilarity>();
    public ICollection<ArticleSimilarity> SimilaritiesAs2 { get; set; } = new List<ArticleSimilarity>();
}