namespace Meezan.Domain.Entities;

public class ScrapeRunLog
{
    public int Id { get; set; }
    /// <summary>"Scheduled" | "Manual"</summary>
    public string TriggeredBy { get; set; } = "Scheduled";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int TotalStocks { get; set; }
    public int SucceededStocks { get; set; }
    public int FailedStocks { get; set; }
    public string? ErrorSummary { get; set; }
}
