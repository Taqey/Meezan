namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record CheckNewFundsResultDto(
    int TotalSourceFunds,
    int TotalDbFunds,
    int ExistingSkippedCount,
    int NewFundsFoundCount,
    List<string> NewFundNames,
    List<string> Failures);
