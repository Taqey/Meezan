using Meezan.Application.Common.Interfaces.Repositories.Funds;

namespace Meezan.Infrastructure.Services.Funds.Sync;

public sealed class FundSyncProgressTracker : IFundSyncProgressTracker
{
    private readonly object _lock = new();
    private bool _isRunning;
    private string? _mode;
    private string? _triggeredBy;
    private DateTime? _startedAtUtc;
    private int _totalFunds;
    private int _processedCount;
    private int _updatedCount;
    private int _failedCount;
    private string? _currentFundCode;
    private string? _currentPhase;
    private DateTime? _finishedAtUtc;
    private string? _lastResultSummary;
    private string? _error;

    public void StartRun(string mode, string triggeredBy, int totalFunds)
    {
        lock (_lock)
        {
            _isRunning = true;
            _mode = mode;
            _triggeredBy = triggeredBy;
            _startedAtUtc = DateTime.UtcNow;
            _totalFunds = totalFunds;
            _processedCount = 0;
            _updatedCount = 0;
            _failedCount = 0;
            _currentFundCode = null;
            _currentPhase = "Starting";
            _finishedAtUtc = null;
            _error = null;
        }
    }

    public void UpdateProgress(int processedCount, int updatedCount, int failedCount, string currentFundCode, string currentPhase)
    {
        lock (_lock)
        {
            _processedCount = processedCount;
            _updatedCount = updatedCount;
            _failedCount = failedCount;
            _currentFundCode = currentFundCode;
            _currentPhase = currentPhase;
        }
    }

    public void SetCompleted(string? summary = null)
    {
        lock (_lock)
        {
            _isRunning = false;
            _finishedAtUtc = DateTime.UtcNow;
            _currentPhase = "Completed";
            _currentFundCode = null;
            _lastResultSummary = summary;
            _error = null;
        }
    }

    public void SetFailed(string error)
    {
        lock (_lock)
        {
            _isRunning = false;
            _finishedAtUtc = DateTime.UtcNow;
            _currentPhase = "Failed";
            _currentFundCode = null;
            _error = error;
            _lastResultSummary = error;
        }
    }

    public FundSyncProgressSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new FundSyncProgressSnapshot(
                IsRunning: _isRunning,
                Mode: _mode,
                TriggeredBy: _triggeredBy,
                StartedAtUtc: _startedAtUtc,
                TotalFunds: _totalFunds,
                ProcessedCount: _processedCount,
                UpdatedCount: _updatedCount,
                FailedCount: _failedCount,
                CurrentFundCode: _currentFundCode,
                CurrentPhase: _currentPhase,
                FinishedAtUtc: _finishedAtUtc,
                LastResultSummary: _lastResultSummary,
                Error: _error);
        }
    }
}
