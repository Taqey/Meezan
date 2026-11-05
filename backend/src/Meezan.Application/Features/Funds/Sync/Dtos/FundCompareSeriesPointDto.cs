namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record FundCompareSeriesPointDto(
    string Date,
    decimal? MyPrice,
    decimal? SourcePrice,
    bool IsMissingInDb,
    bool IsDifferent);

