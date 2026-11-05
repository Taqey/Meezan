namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record HistoricalDataDiffPointDto(
    string Date,
    decimal MyPrice,
    decimal SourcePrice,
    decimal DiffPct);
