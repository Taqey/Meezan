namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record HistoricalDataCommitFundResultDto(
    int FundId,
    string FundCode,
    int InsertedCount,
    int OverwrittenCount,
    bool Success,
    string? Error);

