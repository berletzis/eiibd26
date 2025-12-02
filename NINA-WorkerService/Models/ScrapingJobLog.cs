// Ruta: Models/ScrapingJobLog.cs
namespace NINA_WorkerService.Models;

public class ScrapingJobLog
{
    public int ScrapingJobLogId { get; set; }
    public int ScrapingJobId { get; set; }
    public int? ScrapedPageId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ScrapingJob ScrapingJob { get; set; } = null!;
    public ScrapedPage? ScrapedPage { get; set; }
}