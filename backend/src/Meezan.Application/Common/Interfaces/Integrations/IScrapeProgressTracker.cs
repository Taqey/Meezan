namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>
/// Singleton, in-memory tracker for active and most-recently-completed runs across all jobs.
/// Thread-safe: all mutations are guarded internally.
/// Tracks progress per job key (e.g. "LiveDaily", "SlowQuarterly", "MarketSnapshots").
/// </summary>
public interface IScrapeProgressTracker
{
    // ── Backward-compatible LiveDaily methods ─────────────────────────────────
    void StartRun(string triggeredBy, int totalStocks);
    void RecordStockCompleted(string ticker, bool success);
    void SetCurrentTicker(string ticker);
    void SetCommitting();
    void SetCompleted();
    void SetFailed(string reason);
    ScrapeProgressSnapshot GetSnapshot();

    // ── Multi-job tracking methods ────────────────────────────────────────────
    /// <summary>
    /// Starts a run for a specific jobKey ("LiveDaily", "SlowQuarterly", "MarketSnapshots").
    /// </summary>
    void StartRun(string jobKey, string triggeredBy, int totalItems);

    /// <summary>
    /// Updates the label for the current item being processed (e.g. stock ticker or index/sector slug).
    /// </summary>
    void SetCurrentItem(string jobKey, string itemLabel);

    /// <summary>
    /// Records completion of one item in the given job run.
    /// </summary>
    void RecordItemCompleted(string jobKey, string itemLabel, bool success);

    /// <summary>
    /// Marks the committing phase for the given job.
    /// </summary>
    void SetCommitting(string jobKey);

    /// <summary>
    /// Marks the run for the given job as completed.
    /// </summary>
    void SetCompleted(string jobKey);

    /// <summary>
    /// Marks the run for the given job as failed.
    /// </summary>
    void SetFailed(string jobKey, string reason);

    /// <summary>
    /// Returns the snapshot for a specific jobKey.
    /// </summary>
    ScrapeProgressSnapshot GetSnapshot(string jobKey);

    /// <summary>
    /// Returns snapshots for all tracked jobs.
    /// </summary>
    IReadOnlyDictionary<string, ScrapeProgressSnapshot> GetAllSnapshots();

    /// <summary>
    /// Returns true if any job or a specific job is currently running or committing.
    /// </summary>
    bool IsRunning(string? jobKey = null);
}

public enum ScrapeRunStatus
{
    NotRunning,
    Running,
    Committing,
    Completed,
    Failed
}

/// <summary>
/// Immutable snapshot returned to callers — no lock needed once constructed.
/// Derived fields are pre-computed server-side so the frontend can bind directly.
/// </summary>
public sealed record ScrapeProgressSnapshot(
    ScrapeRunStatus Status,
    string? TriggeredBy,
    int TotalStocks,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    double PercentComplete,
    string? CurrentStockTicker,
    DateTime? StartedAt,
    double ElapsedSeconds,
    double? EstimatedSecondsRemaining,
    string JobKey = "LiveDaily",
    DateTime? FinishedAt = null,
    string? ErrorReason = null
);
