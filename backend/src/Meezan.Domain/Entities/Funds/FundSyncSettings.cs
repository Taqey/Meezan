namespace Meezan.Domain.Entities;

/// <summary>
/// Fund sync job settings (schedule, enable/disable, etc.).
/// </summary>
public class FundSyncSettings
{
    public int Id { get; set; } = 1; // singleton

    public bool IsEnabled { get; set; } = true;
    public TimeOnly RunTimeCairo { get; set; } = new TimeOnly(0, 0); // 00:00 Cairo
    public bool PreventOverlap { get; set; } = true;
    public int RequestDelaySeconds { get; set; } = 2; // delay between fund requests
    public int MaxRetries { get; set; } = 3;
    public int RetryBackoffSeconds { get; set; } = 5;

    /// <summary>
    /// When true, funds belonging to index-tracking categories (IsIndexTracking = true)
    /// automatically receive the "index" fund type unless manually overridden. Default: true.
    /// </summary>
    public bool AutoIndexFundTypeEnabled { get; set; } = true;

    /// <summary>
    /// Master switch for automatic classification keyword rules (classification tree).
    /// Default ON. When OFF, keyword-driven classification is skipped; manual
    /// assignments are preserved and the SNDUK baseline mapping applies.
    /// </summary>
    public bool AutoClassificationEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
}

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

public enum CategoryChangeAction
{
    AutoAdded = 0,
    AutoRemoved = 1,
    ManualIncluded = 2,
    ManualExcluded = 3,
    ManualIncludeRemoved = 4,
    ManualExcludeRemoved = 5,
    ResetToAuto = 6,
    CategoryCreated = 7,
    CategoryRenamed = 8,
    CategoryDeleted = 9,
    CategoryRestored = 10,
    CategoryMerged = 11
}