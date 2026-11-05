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
