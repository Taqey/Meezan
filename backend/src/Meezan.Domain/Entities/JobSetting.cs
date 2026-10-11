namespace Meezan.Domain.Entities;

/// <summary>
/// Persisted enable/disable flag for each background job.
/// Keyed by a stable string like "LiveDaily", "SlowQuarterly", "MarketSnapshots".
/// Survives restarts and deployments.
/// </summary>
public class JobSetting
{
    public int Id { get; set; }

    /// <summary>Stable key used in the API and frontend (e.g. "LiveDaily").</summary>
    public string JobKey { get; set; } = string.Empty;

    /// <summary>When false the background worker skips its scheduled run. Manual runs still work.</summary>
    public bool Enabled { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
