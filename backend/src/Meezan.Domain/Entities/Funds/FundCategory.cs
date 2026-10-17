namespace Meezan.Domain.Entities;

/// <summary>
/// Fund category for filtering (e.g., Sharia-compliant, EGX30 tracker, sector, etc.).
/// Can be auto-derived or manually created.
/// </summary>
public class FundCategory
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty; // stable URL identifier
    
    // Visual
    public string Icon { get; set; } = string.Empty; // lucide icon name
    public string Color { get; set; } = string.Empty; // hex color
    
    // Auto-derivation rule
    public bool IsAutoDerived { get; set; }
    public string AutoRuleJson { get; set; } = string.Empty; // JSON: { "keywords": ["EGX30"], "field": "name|description|strategy|type" }
    
    /// <summary>
    /// When true, funds in this category automatically receive the "index" (صندوق مؤشر) type
    /// unless manually overridden.
    /// </summary>
    public bool IsIndexTracking { get; set; }
    
    // Admin control
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string DeletedReason { get; set; } = string.Empty;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    
    // Navigation
    public ICollection<FundCategoryOverride> Overrides { get; set; } = new List<FundCategoryOverride>();
}

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

public enum OverrideAction
{
    Include = 1,
    Exclude = 2
}