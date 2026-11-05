namespace Meezan.Domain.Entities;

/// <summary>
/// Manual override for fund-category membership.
/// Action: Include = force include, Exclude = force exclude (manual wins over auto).
/// </summary>
public class FundCategoryOverride
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public int CategoryId { get; set; }

    public OverrideAction Action { get; set; } // Include or Exclude
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;

    public Fund? Fund { get; set; }
    public FundCategory? Category { get; set; }
}
