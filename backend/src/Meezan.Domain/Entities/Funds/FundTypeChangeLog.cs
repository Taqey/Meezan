namespace Meezan.Domain.Entities;

/// <summary>
/// Audit trail for type administration. FundId is null for category-level actions.
/// </summary>
public class FundTypeChangeLog
{
    public int Id { get; set; }
    public int? FundId { get; set; }
    public int? FundTypeDefinitionId { get; set; }
    public FundTypeChangeAction Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
    public FundTypeDefinition? Definition { get; set; }
}
