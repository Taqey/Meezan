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
    public int DurationSeconds { get; set; }
    public ICollection<FundSyncLogDetail> Details { get; set; } = new List<FundSyncLogDetail>();
}

/// <summary>
/// Per-fund sync log for detailed error tracking.
/// </summary>
public class FundSyncLogDetail
{
    public int Id { get; set; }
    public int FundSyncLogId { get; set; }
    public int FundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public FundSyncDetailStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public FundSyncLog? FundSyncLog { get; set; }
}

public enum FundSyncStatus
{
    Running = 0,
    Success = 1,
    Partial = 2, // some funds failed
    Failed = 3
}

public enum FundSyncDetailStatus
{
    Success = 0,
    Failed = 1,
    Skipped = 2
}