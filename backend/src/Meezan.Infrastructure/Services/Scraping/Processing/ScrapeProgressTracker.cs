using Meezan.Application.Common.Interfaces.Integrations;

namespace Meezan.Infrastructure.Services.Scraping.Processing;

/// <summary>
/// Singleton, thread-safe in-memory implementation of <see cref="IScrapeProgressTracker"/>.
/// Tracks state for multiple jobs (LiveDaily, SlowQuarterly, MarketSnapshots) simultaneously.
/// </summary>
public sealed class ScrapeProgressTracker : IScrapeProgressTracker
{
    private const int MinItemsForEstimate = 5;

    private class JobRunState
    {
        public string JobKey { get; }
        public ScrapeRunStatus Status { get; set; } = ScrapeRunStatus.NotRunning;
        public string? TriggeredBy { get; set; }
        public int TotalItems { get; set; }
        public int ProcessedCount { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public string? CurrentItem { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? ErrorReason { get; set; }

        public JobRunState(string jobKey)
        {
            JobKey = jobKey;
        }

        public ScrapeProgressSnapshot ToSnapshot()
        {
            var now = DateTime.UtcNow;
            var elapsedSec = StartedAt.HasValue
                ? (FinishedAt.HasValue ? FinishedAt.Value - StartedAt.Value : now - StartedAt.Value).TotalSeconds
                : 0.0;

            var pct = TotalItems > 0
                ? Math.Round(ProcessedCount / (double)TotalItems * 100.0, 1)
                : (Status == ScrapeRunStatus.Completed ? 100.0 : 0.0);

            double? etaSeconds = null;
            if (Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing &&
                ProcessedCount >= MinItemsForEstimate && elapsedSec > 0 && TotalItems > ProcessedCount)
            {
                var avgPerItem = elapsedSec / ProcessedCount;
                etaSeconds = Math.Round(avgPerItem * (TotalItems - ProcessedCount), 0);
            }

            return new ScrapeProgressSnapshot(
                Status: Status,
                TriggeredBy: TriggeredBy,
                TotalStocks: TotalItems,
                ProcessedCount: ProcessedCount,
                SucceededCount: SucceededCount,
                FailedCount: FailedCount,
                PercentComplete: pct,
                CurrentStockTicker: CurrentItem,
                StartedAt: StartedAt,
                ElapsedSeconds: Math.Round(elapsedSec, 1),
                EstimatedSecondsRemaining: etaSeconds,
                JobKey: JobKey,
                FinishedAt: FinishedAt,
                ErrorReason: ErrorReason
            );
        }
    }

    private readonly object _lock = new();
    private readonly Dictionary<string, JobRunState> _jobs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LiveDaily"] = new("LiveDaily"),
        ["SlowQuarterly"] = new("SlowQuarterly"),
        ["MarketSnapshots"] = new("MarketSnapshots")
    };

    private JobRunState GetOrAdd(string jobKey)
    {
        if (!_jobs.TryGetValue(jobKey, out var state))
        {
            state = new JobRunState(jobKey);
            _jobs[jobKey] = state;
        }
        return state;
    }

    // ── Backward-compatible LiveDaily delegates ───────────────────────────────

    public void StartRun(string triggeredBy, int totalStocks)
    {
        StartRun("LiveDaily", triggeredBy, totalStocks);
    }

    public void SetCurrentTicker(string ticker)
    {
        SetCurrentItem("LiveDaily", ticker);
    }

    public void RecordStockCompleted(string ticker, bool success)
    {
        RecordItemCompleted("LiveDaily", ticker, success);
    }

    public void SetCommitting()
    {
        SetCommitting("LiveDaily");
    }

    public void SetCompleted()
    {
        SetCompleted("LiveDaily");
    }

    public void SetFailed(string reason)
    {
        SetFailed("LiveDaily", reason);
    }

    public ScrapeProgressSnapshot GetSnapshot()
    {
        // By default, if another job is running, return that one; otherwise LiveDaily
        lock (_lock)
        {
            var running = _jobs.Values.FirstOrDefault(j => j.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing);
            if (running != null) return running.ToSnapshot();
            return GetOrAdd("LiveDaily").ToSnapshot();
        }
    }

    // ── Multi-job implementation ──────────────────────────────────────────────

    public void StartRun(string jobKey, string triggeredBy, int totalItems)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.Status = ScrapeRunStatus.Running;
            job.TriggeredBy = triggeredBy;
            job.TotalItems = totalItems;
            job.ProcessedCount = 0;
            job.SucceededCount = 0;
            job.FailedCount = 0;
            job.CurrentItem = null;
            job.StartedAt = DateTime.UtcNow;
            job.FinishedAt = null;
            job.ErrorReason = null;
        }
    }

    public void SetCurrentItem(string jobKey, string itemLabel)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.CurrentItem = itemLabel;
        }
    }

    public void RecordItemCompleted(string jobKey, string itemLabel, bool success)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.ProcessedCount++;
            if (success) job.SucceededCount++;
            else job.FailedCount++;

            if (job.CurrentItem == itemLabel)
            {
                job.CurrentItem = null;
            }
        }
    }

    public void SetCommitting(string jobKey)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.Status = ScrapeRunStatus.Committing;
            job.CurrentItem = null;
        }
    }

    public void SetCompleted(string jobKey)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.Status = ScrapeRunStatus.Completed;
            job.FinishedAt = DateTime.UtcNow;
            job.CurrentItem = null;
        }
    }

    public void SetFailed(string jobKey, string reason)
    {
        lock (_lock)
        {
            var job = GetOrAdd(jobKey);
            job.Status = ScrapeRunStatus.Failed;
            job.FinishedAt = DateTime.UtcNow;
            job.ErrorReason = reason;
            job.CurrentItem = null;
        }
    }

    public ScrapeProgressSnapshot GetSnapshot(string jobKey)
    {
        lock (_lock)
        {
            return GetOrAdd(jobKey).ToSnapshot();
        }
    }

    public IReadOnlyDictionary<string, ScrapeProgressSnapshot> GetAllSnapshots()
    {
        lock (_lock)
        {
            return _jobs.ToDictionary(k => k.Key, v => v.Value.ToSnapshot(), StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool IsRunning(string? jobKey = null)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(jobKey))
            {
                return _jobs.TryGetValue(jobKey, out var job) &&
                       job.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing;
            }
            return _jobs.Values.Any(j => j.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing);
        }
    }
}
