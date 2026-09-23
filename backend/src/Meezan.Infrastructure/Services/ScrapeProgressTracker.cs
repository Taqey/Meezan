using Meezan.Application.Common.Interfaces;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Singleton, thread-safe in-memory implementation of <see cref="IScrapeProgressTracker"/>.
/// All mutable state is guarded by a single lock so concurrent reads from the
/// polling endpoint never race with writes from the scrape loop.
/// </summary>
public sealed class ScrapeProgressTracker : IScrapeProgressTracker
{
    // Minimum stocks processed before we trust the ETA estimate enough to surface it.
    private const int MinStocksForEstimate = 5;

    private readonly object _lock = new();

    // ── Mutable state (always accessed under _lock) ───────────────────────────
    private ScrapeRunStatus _status = ScrapeRunStatus.NotRunning;
    private string? _triggeredBy;
    private int _totalStocks;
    private int _processedCount;
    private int _succeededCount;
    private int _failedCount;
    private string? _currentTicker;
    private DateTime? _startedAt;

    // ── IScrapeProgressTracker ────────────────────────────────────────────────

    public void StartRun(string triggeredBy, int totalStocks)
    {
        lock (_lock)
        {
            _status = ScrapeRunStatus.Running;
            _triggeredBy = triggeredBy;
            _totalStocks = totalStocks;
            _processedCount = 0;
            _succeededCount = 0;
            _failedCount = 0;
            _currentTicker = null;
            _startedAt = DateTime.UtcNow;
        }
    }

    public void SetCurrentTicker(string ticker)
    {
        lock (_lock) { _currentTicker = ticker; }
    }

    public void RecordStockCompleted(string ticker, bool success)
    {
        lock (_lock)
        {
            _processedCount++;
            if (success) _succeededCount++;
            else _failedCount++;
            // Clear the "currently scraping" ticker once done
            if (_currentTicker == ticker) _currentTicker = null;
        }
    }

    public void SetCommitting()
    {
        lock (_lock) { _status = ScrapeRunStatus.Committing; _currentTicker = null; }
    }

    public void SetCompleted()
    {
        lock (_lock) { _status = ScrapeRunStatus.Completed; _currentTicker = null; }
    }

    public void SetFailed(string reason)
    {
        lock (_lock) { _status = ScrapeRunStatus.Failed; _currentTicker = null; }
    }

    public ScrapeProgressSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsedSec = _startedAt.HasValue
                ? (now - _startedAt.Value).TotalSeconds
                : 0.0;

            var pct = _totalStocks > 0
                ? Math.Round(_processedCount / (double)_totalStocks * 100.0, 1)
                : 0.0;

            double? etaSeconds = null;
            if (_processedCount >= MinStocksForEstimate && elapsedSec > 0 && _totalStocks > _processedCount)
            {
                var avgPerStock = elapsedSec / _processedCount;
                etaSeconds = Math.Round(avgPerStock * (_totalStocks - _processedCount), 0);
            }

            return new ScrapeProgressSnapshot(
                Status: _status,
                TriggeredBy: _triggeredBy,
                TotalStocks: _totalStocks,
                ProcessedCount: _processedCount,
                SucceededCount: _succeededCount,
                FailedCount: _failedCount,
                PercentComplete: pct,
                CurrentStockTicker: _currentTicker,
                StartedAt: _startedAt,
                ElapsedSeconds: Math.Round(elapsedSec, 1),
                EstimatedSecondsRemaining: etaSeconds
            );
        }
    }
}
