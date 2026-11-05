namespace Meezan.Application.Common.Interfaces.Repositories.Funds;

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
