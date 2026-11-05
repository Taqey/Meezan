namespace Meezan.Domain.Entities;

/// <summary>
/// Per-fund manual classification assignment. Manual overrides always win over auto.
/// </summary>
public class FundClassificationOverride
{
    public int Id { get; set; }

    public int FundId { get; set; }
    public Fund? Fund { get; set; }

    public int NodeId { get; set; }
    public FundClassificationNode? Node { get; set; }

    /// <summary>
    /// Include = force include in this node (manual win).
    /// Exclude = force exclude from this node (manual win).
    /// ResetToAuto = remove manual override, let auto rules decide.
    /// </summary>
    public ClassificationOverrideAction Action { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}
