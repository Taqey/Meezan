namespace Meezan.Domain.Entities;

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
