namespace NINA_WorkerService.Models;

public class ArticleBoost
{
    public int ArticleBoostId { get; set; }
    public int ArticleId { get; set; }
    public decimal BoostScore { get; set; }
    public string? Reason { get; set; }
    public DateTime CalculatedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? AlgorithmVersion { get; set; }

    public Article Article { get; set; } = null!;
}