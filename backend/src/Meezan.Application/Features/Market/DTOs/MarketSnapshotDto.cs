namespace Meezan.Application.Features.Market.DTOs;

public record MarketSnapshotDto(
    string Kind,
    string Code,
    string MubasherCode,
    decimal? Value,
    decimal? Change,
    decimal? ChangePct,
    string? SourceDateText,
    string Source,
    DateTime FetchedAt
);
