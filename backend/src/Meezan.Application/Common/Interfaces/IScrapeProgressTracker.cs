namespace Meezan.Application.Common.Interfaces;

/// <summary>
/// Singleton, in-memory tracker for the currently-active (or most-recently-completed) scrape run.
/// Never persisted to the database — transient across restarts.
/// Thread-safe: all mutations are guarded internally.
/// </summary>
public interface IScrapeProgressTracker
{
    /// <summary>
    /// Resets the tracker and marks the start of a new run.
    /// Must be called before any stock is processed.
    /// </summary>
    void StartRun(string triggeredBy, int totalStocks);

    /// <summary>
    /// Records that one stock finished (successfully or not) and updates
    /// all derived fields (PercentComplete, EstimatedSecondsRemaining).
    /// </summary>
    void RecordStockCompleted(string ticker, bool success);

    /// <summary>
    /// Sets the ticker that is currently being scraped.
    /// Called immediately before issuing the HTTP scrape for each stock.
    /// </summary>
    void SetCurrentTicker(string ticker);

    /// <summary>
    /// Transitions status to Committing (during the atomic DB write phase).
    /// </summary>
    void SetCommitting();

    /// <summary>
    /// Marks the run as fully completed (success path).
    /// </summary>
    void SetCompleted();

    /// <summary>
    /// Marks the run as failed.
    /// </summary>
    void SetFailed(string reason);

    /// <summary>
    /// Returns a point-in-time snapshot of current progress.
    /// Reads are lock-free (snapshot copy); safe to call from any thread.
    /// </summary>
    ScrapeProgressSnapshot GetSnapshot();
}

// ── Value objects ─────────────────────────────────────────────────────────────

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
/// All derived fields (PercentComplete, ElapsedSeconds, EstimatedSecondsRemaining)
/// are pre-computed server-side so the frontend can bind them directly.
/// </summary>
public sealed record ScrapeProgressSnapshot(
    ScrapeRunStatus Status,
    string? TriggeredBy,
    int TotalStocks,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    double PercentComplete,          // 0.0–100.0, rounded to 1 decimal
    string? CurrentStockTicker,
    DateTime? StartedAt,
    double ElapsedSeconds,           // now − StartedAt, pre-computed
    double? EstimatedSecondsRemaining // null until MinStocksForEstimate have completed
);
