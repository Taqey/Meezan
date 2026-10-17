namespace Meezan.Domain.Entities;

/// <summary>
/// Generic editable classification tree node for investment funds.
/// Supports unlimited depth (max 6), slug uniqueness, soft delete, and editable rules.
/// </summary>
public class FundClassificationNode
{
    public int Id { get; set; }

    /// <summary>Parent node in the hierarchy. Null for root nodes.</summary>
    public int? ParentId { get; set; }
    public FundClassificationNode? Parent { get; set; }
    public ICollection<FundClassificationNode> Children { get; set; } = new List<FundClassificationNode>();

    /// <summary>Depth in the tree (0 = root). Max 6 levels enforced.</summary>
    public int Level { get; set; }

    /// <summary>Stable URL identifier. Must be unique across the entire tree (including soft-deleted).</summary>
    public string Slug { get; set; } = string.Empty;

    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

    /// <summary>Optional Lucide icon name.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Hex color for UI theming (e.g., #7c3aed).</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Display order among siblings.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Whether this node appears in public filter UI.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Soft delete flag.</summary>
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string DeletedReason { get; set; } = string.Empty;

    /// <summary>
    /// When true, funds assigned to this node must also select a child node.
    /// Used for nodes like "Equity" that require an Index/Sector/Active sub-selection.
    /// </summary>
    public bool RequiresChildSelection { get; set; } = true;

    /// <summary>Label for child filter in public UI (Arabic).</summary>
    public string ChildrenLabelAr { get; set; } = string.Empty;

    /// <summary>Label for child filter in public UI (English).</summary>
    public string ChildrenLabelEn { get; set; } = string.Empty;

    /// <summary>Optional automatic classification rule (keywords, field, or rule type).</summary>
    public string AutoRuleJson { get; set; } = string.Empty;

    /// <summary>Whether automatic classification is enabled for this node.</summary>
    public bool AutoClassificationEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation
    public ICollection<FundClassificationOverride> Overrides { get; set; } = new List<FundClassificationOverride>();
    public ICollection<FundClassificationChangeLog> ChangeLogs { get; set; } = new List<FundClassificationChangeLog>();

    /// <summary>Computed depth (for validation). Max 6 levels total.</summary>
    public const int MaxDepth = 6;
}

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

/// <summary>
/// Action for a manual classification override.
/// </summary>
public enum ClassificationOverrideAction
{
    Include = 1,
    Exclude = 2,
    ResetToAuto = 3
}

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

public enum ClassificationChangeAction
{
    AutoAdded = 0,
    AutoRemoved = 1,
    ManualIncluded = 2,
    ManualExcluded = 3,
    ManualIncludeRemoved = 4,
    ManualExcludeRemoved = 5,
    ResetToAuto = 6,
    NodeCreated = 7,
    NodeRenamed = 8,
    NodeDeleted = 9,
    NodeRestored = 10,
    NodeMerged = 11,
    FundAssigned = 12,
    FundRemoved = 13,
    FundOverridden = 14,
    FundOverrideRemoved = 15,
    NodeMoved = 16,
    NodeReordered = 17,
    VisibilityChanged = 18,
    RuleChanged = 19,
    BulkAssigned = 20,
    BulkRemoved = 21,
    Reclassified = 22
}