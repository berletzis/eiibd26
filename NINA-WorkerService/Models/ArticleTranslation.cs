namespace NINA_WorkerService.Models;

public class ArticleTranslation
{
    public int ArticleTranslationId { get; set; }
    public int ArticleId { get; set; }
    public string SourceLanguage { get; set; } = null!;
    public string TargetLanguage { get; set; } = null!;
    public string TranslatedTitle { get; set; } = null!;
    public string TranslatedContent { get; set; } = null!;
    public string? TranslationProvider { get; set; }
    public decimal? QualityScore { get; set; }
    public DateTime CreatedAt { get; set; }

    public Article Article { get; set; } = null!;
}