namespace NINA_WorkerService.Models;

public class ArticleSimilarity
{
    public int ArticleId1 { get; set; }
    public int ArticleId2 { get; set; }
    public decimal SimilarityScore { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string? Method { get; set; }

    public Article Article1 { get; set; } = null!;
    public Article Article2 { get; set; } = null!;
}