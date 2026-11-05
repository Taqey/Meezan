namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record HistoricalDataCommitResultDto(
    int TotalFundsProcessed,
    int TotalInsertedPoints,
    int TotalOverwrittenPoints,
    List<HistoricalDataCommitFundResultDto> Funds);

