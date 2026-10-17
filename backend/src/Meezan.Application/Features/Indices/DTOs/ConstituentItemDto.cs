namespace Meezan.Application.Features.Indices.DTOs;

public record ConstituentItemDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    List<string> Indices,
    string? ShariahStatus,
    List<Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto> ShariahOpinions,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    decimal? Weight,
    string? Currency,
    string? SectorNameAr = null,
    bool HasShariahBoard = false,
    string? ShariahBoardNote = null
);
