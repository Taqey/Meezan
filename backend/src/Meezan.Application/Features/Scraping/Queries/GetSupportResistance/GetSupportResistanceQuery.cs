using MediatR;

namespace Meezan.Application.Features.Scraping.Queries.GetSupportResistance;

public record GetSupportResistanceQuery(string Ticker) : IRequest<SupportResistanceDto?>;

public record SupportResistanceDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    decimal? LastPrice,
    decimal? ChangePct,
    decimal? Pivot,
    decimal? R1,
    decimal? R2,
    decimal? S1,
    decimal? S2,
    DateTime? FetchedAt
);
