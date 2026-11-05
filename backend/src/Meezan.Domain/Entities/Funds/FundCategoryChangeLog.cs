namespace Meezan.Domain.Entities;

/// <summary>
/// Log of manual category changes for audit trail.
/// </summary>
public class FundCategoryChangeLog
{
    public int Id { get; set; }
    /// <summary>Null for category-level operations (create/rename/delete/restore/merge).</summary>
    public int? FundId { get; set; }
    public int CategoryId { get; set; }
    public CategoryChangeAction Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
    public FundCategory? Category { get; set; }
}
