namespace Meezan.Domain.Entities;

/// <summary>
/// Editable fund type (صندوق أسهم، دخل ثابت، ...). The slug is the stable identity
/// and maps 1:1 to the <see cref="FundType"/> enum; AR/EN labels, order and
/// visibility are operator-editable and survive every sync.
/// </summary>
public class FundTypeDefinition
{
    public int Id { get; set; }

    /// <summary>Stable key, e.g. "equity", "fixed_income". Matches the enum name.</summary>
    public string Slug { get; set; } = string.Empty;

    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedReason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<FundTypeOverride> Overrides { get; set; } = new List<FundTypeOverride>();
}

/// <summary>
/// Per-fund manual type assignment. The sync writes <see cref="Fund.Type"/> from
/// SNDUK on every run but NEVER touches this table, so an operator decision wins.
/// </summary>
public class FundTypeOverride
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public int FundTypeDefinitionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;

    public Fund? Fund { get; set; }
    public FundTypeDefinition? Definition { get; set; }
}

public enum FundTypeChangeAction
{
    Created = 0,
    Renamed = 1,
    Deleted = 2,
    Restored = 3,
    Merged = 4,
    Reordered = 5,
    VisibilityChanged = 6,
    FundOverridden = 7,
    FundOverrideRemoved = 8
}

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