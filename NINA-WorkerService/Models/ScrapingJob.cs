// Ruta: Models/ScrapingJob.cs
namespace NINA_WorkerService.Models;

public class ScrapingJob
{
    public int ScrapingJobId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = null!;
    public int? TotalPages { get; set; }
    public string? Notes { get; set; }

    public ICollection<ScrapingJobLog> Logs { get; set; } = new List<ScrapingJobLog>();
}