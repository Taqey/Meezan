using MediatR;
using Meezan.Application.Features.Shariah.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetMarketData;

public record GetMarketDataQuery(string Ticker) : IRequest<MarketDataDto?>;

public record MarketDataDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string? SectorNameAr,
    string? SectorNameEn,
    List<IndexInStockDto> Indices,
    string? ShariahStatus,
    decimal? ShariahPct,
    List<ShariahSourceOpinionDto> ShariahOpinions,
    decimal? NominalValue,
    decimal? MarketValue,
    decimal? BookValue,
    decimal? PbRatio,
    decimal? Eps,
    decimal? PeRatio,
    string? Currency,
    decimal? High,
    decimal? Low,
    decimal? Open,
    decimal? ClosingPrice,
    string? SourceLastUpdateText,
    DateTime? FetchedAt,
    // Fair value
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiff,
    decimal? FairValueDiffPct,
    int? MethodsUsedCount,
    int? MethodsExcludedCount,
    string? ValuationConfidence,
    List<FairValueMethodDto>? FairValueMethods,
    StockShariahMetricsDto? ShariahMetrics = null
);

public record IndexInStockDto(
    string Code,
    string NameAr,
    string NameEn,
    decimal Weight
);

public record FairValueMethodDto(
    string Name,
    decimal Value,
    bool IsOutlier
);
