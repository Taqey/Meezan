namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundSyncProgressTracker
{
    void StartRun(string mode, string triggeredBy, int totalFunds);
    void UpdateProgress(int processedCount, int updatedCount, int failedCount, string currentFundCode, string currentPhase);
    void SetCompleted(string? summary = null);
    void SetFailed(string error);
    FundSyncProgressSnapshot GetSnapshot();
}

public sealed record FundSyncProgressSnapshot(
    bool IsRunning,
    string? Mode,
    string? TriggeredBy,
    DateTime? StartedAtUtc,
    int TotalFunds,
    int ProcessedCount,
    int UpdatedCount,
    int FailedCount,
    string? CurrentFundCode,
    string? CurrentPhase,
    DateTime? FinishedAtUtc,
    string? LastResultSummary,
    string? Error);
