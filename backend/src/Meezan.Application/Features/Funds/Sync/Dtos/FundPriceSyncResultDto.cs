namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record FundPriceSyncResultDto(
    int TotalSndukFunds,
    int UpdatedFunds,
    int MissingFromSndukCount,
    int NewInSndukCount,
    double DurationSeconds,
    DateTime SyncedAtUtc,
    string? Message = null);
