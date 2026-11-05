namespace Meezan.Domain.Entities;

/// <summary>
/// Fund sync job settings (schedule, enable/disable, etc.).
/// </summary>
public class FundSyncSettings
{
    public int Id { get; set; } = 1; // singleton

    public bool IsEnabled { get; set; } = true;
    public TimeOnly RunTimeCairo { get; set; } = new TimeOnly(0, 0); // 00:00 Cairo
    public bool FullSyncScheduleEnabled { get; set; } = false; // OFF by default
    public TimeOnly FullSyncRunTimeCairo { get; set; } = new TimeOnly(2, 0); // optional 02:00 Cairo
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
