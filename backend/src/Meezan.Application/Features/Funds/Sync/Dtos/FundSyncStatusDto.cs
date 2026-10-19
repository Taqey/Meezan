namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record FundSyncStatusDto(
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
