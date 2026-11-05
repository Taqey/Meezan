namespace Meezan.Domain.Entities;

/// <summary>
/// Audit trail for classification changes.
/// </summary>
public class FundClassificationChangeLog
{
    public int Id { get; set; }

    public int? FundId { get; set; }
    public Fund? Fund { get; set; }

    public int? NodeId { get; set; }
    public FundClassificationNode? Node { get; set; }

    public ClassificationChangeAction Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
