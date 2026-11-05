using System.Collections.Generic;

namespace Meezan.Application.Common.Interfaces.Repositories.Indices;

public record ConstituentProjection(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string? IndexCodes,
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
    /// <summary>True when the stock is overseen by a Shariah board/committee.</summary>
    bool HasShariahBoard = false,
    decimal? MarketCap = null
);
