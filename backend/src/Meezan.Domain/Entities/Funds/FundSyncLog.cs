namespace Meezan.Domain.Entities;

/// <summary>
/// Log of fund sync job runs.
/// </summary>
public class FundSyncLog
{
    public int Id { get; set; }
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public FundSyncStatus Status { get; set; }
    public int TotalFunds { get; set; }
    public int UpdatedFunds { get; set; }
    public int FailedFunds { get; set; }
    public int SkippedFunds { get; set; }
    public string? ErrorSummary { get; set; }
    public string TriggeredBy { get; set; } = "Scheduled"; // "Scheduled" or "Manual"
    public string Mode { get; set; } = "Full"; // "Prices" or "Full"
    public int DurationSeconds { get; set; }
    public ICollection<FundSyncLogDetail> Details { get; set; } = new List<FundSyncLogDetail>();
}
