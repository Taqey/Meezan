namespace Meezan.Domain.Entities;

/// <summary>
/// Immutable backup of every fund's classification state before the hierarchical
/// tree migration. One row per fund. Never written by sync; kept for verification
/// and recovery. Do not destroy after migration.
/// </summary>
public class FundClassificationBackup
{
    public int Id { get; set; }

    public int FundId { get; set; }
    public string FundCode { get; set; } = string.Empty;
    public string? FundNameAr { get; set; }
    public string? FundNameEn { get; set; }

    /// <summary>Raw SNDUK-derived enum value at backup time.</summary>
    public int OldType { get; set; }
    public string OldTypeSlug { get; set; } = string.Empty;

    /// <summary>Manual type override slug at backup time, if any.</summary>
    public string? OldTypeOverrideSlug { get; set; }

    /// <summary>Category slugs the fund belonged to (final membership).</summary>
    public string OldCategorySlugs { get; set; } = string.Empty;

    /// <summary>Manual category include slugs (CreatedBy != AutoSync, Action = Include).</summary>
    public string OldManualIncludeSlugs { get; set; } = string.Empty;

    /// <summary>Manual category exclude slugs (CreatedBy != AutoSync, Action = Exclude).</summary>
    public string OldManualExcludeSlugs { get; set; } = string.Empty;

    /// <summary>Effective classification path assigned by the migration (filled during migration).</summary>
    public string? NewPath { get; set; }

    /// <summary>Why the new path was chosen (manual/auto/SNDUK baseline + rule).</summary>
    public string? MigrationReason { get; set; }

    public DateTime BackedUpAt { get; set; } = DateTime.UtcNow;
    public string BackedUpBy { get; set; } = string.Empty;
}
