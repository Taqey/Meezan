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
    /// <summary>
    /// True when this stock has a full market-data record (price, ratios, fair value etc.).
    /// False for "board exists only" stocks that appear in the Shariah source but have no
    /// Mubasher trading page yet. When false, all market-data fields below are null.
    /// </summary>
    bool HasMarketData,
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
    /// <summary>
    /// "Cheap" | "Expensive" | "Fair" | "Unavailable". Null only when no fair-value row
    /// exists at all (stock never had valuation inputs scraped). "Unavailable" means no
    /// trustworthy fair value could be computed: zero applicable methods, nothing survived
    /// the outlier fallback, or the stock's own price is missing/zero — FairValue,
    /// FairValueDiff and FairValueDiffPct are all null in that state.
    /// </summary>
    string? PriceComparison,
    decimal? FairValueDiff,
    decimal? FairValueDiffPct,
    int? MethodsUsedCount,
    int? MethodsExcludedCount,
    string? ValuationConfidence,
    List<FairValueMethodDto>? FairValueMethods,
    StockShariahMetricsDto? ShariahMetrics = null,
    /// <summary>
    /// True when the stock is overseen by its own Shariah board/committee — a plain
    /// "لجنة شرعية" and an accredited "هيئة رقابة شرعية داخلية معتمدة" are the same case.
    /// For these stocks ShariahPct is null, ShariahOpinions is empty and ShariahMetrics
    /// is null: only the status plus ShariahBoardNote are surfaced. The frontend renders
    /// the simplified board panel instead of the full 7-source opinion grid.
    /// </summary>
    bool HasShariahBoard = false,
    /// <summary>
    /// Unified board-supervision note shown for every board-governed stock (identical
    /// wording for both board descriptions).
    /// </summary>
    string? ShariahBoardNote = null,
    /// <summary>
    /// First-screening gate: the company's own line of business (نشاط الشركة).
    /// false = the activity itself is prohibited, which is an automatic standalone
    /// disqualification. For those stocks this DTO returns an empty ShariahOpinions,
    /// a null ShariahPct and a null ShariahMetrics — no board opinions, no purification
    /// % and no AAOIFI/S&amp;P ratios are assembled. Null = unknown (no metrics row).
    /// </summary>
    bool? ActivityCompliant = null,
    /// <summary>
    /// Business-activity classification behind the verdict, e.g. "خدمات مالية / تخصيص".
    /// Always populated when the metrics row exists, even though ShariahMetrics is null
    /// for activity-non-compliant stocks.
    /// </summary>
    string? ActivityClassification = null
);

public record IndexInStockDto(
    string Code,
    string NameAr,
    string NameEn,
    decimal? Weight
);

public record FairValueMethodDto(
    string Name,
    /// <summary>Null when the method produced no usable estimate — never defaulted to 0.</summary>
    decimal? Value,
    bool IsOutlier
);
