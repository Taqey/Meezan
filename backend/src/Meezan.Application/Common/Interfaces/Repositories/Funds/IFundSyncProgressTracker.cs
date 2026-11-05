namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

public interface IFundSyncProgressTracker
{
    void StartRun(string mode, string triggeredBy, int totalFunds);
    void UpdateProgress(int processedCount, int updatedCount, int failedCount, string currentFundCode, string currentPhase);
    void SetCompleted(string? summary = null);
    void SetFailed(string error);
    FundSyncProgressSnapshot GetSnapshot();
}
