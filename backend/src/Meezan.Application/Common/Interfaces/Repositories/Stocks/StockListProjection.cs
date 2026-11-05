namespace Meezan.Application.Common.Interfaces.Repositories.Stocks;

/// <summary>
/// Flat projection returned from QueryPagedAsync.
/// Index codes are pre-joined as a comma-separated string and split by the handler.
/// </summary>
public record StockListProjection(
    string Ticker,
    string? NameAr,
    string? NameEn,
    string? IndexCodes,    // pipe-delimited e.g. "EGX30|Sectoral-Indices" — split by handler
    string? ShariahStatus,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    string? Currency,        // null means EGP (جنيه مصري)
    string? SectorNameAr = null,
    decimal? PeRatio = null,
    decimal? PbRatio = null,
    /// <summary>
    /// Weight within <see cref="WeightIndexCode"/> (null when the stock has no
    /// constituent row in that index, e.g. weight-less source files).
    /// Used by the sector page cards + weight sorting.
    /// </summary>
    decimal? Weight = null,
    /// <summary>
    /// Index code the <see cref="Weight"/> belongs to: the single selected index
    /// when the request targets exactly one, else "SECTORAL-INDICES".
    /// </summary>
    string? WeightIndexCode = null,
    /// <summary>True when the stock is capped at 35% in its sector index calculation.</summary>
    bool? IsCapped = null,
    /// <summary>The capping factor applied to the stock's market cap if capped.</summary>
    decimal? CappingFactor = null,
    bool? CoreActivityCompliant = null,
    string? CategoryAr = null,
    string? CategoryEn = null,
    decimal? SpHaramEarningPercentage = null,
    decimal? LoansPercentage = null,
    decimal? PurificationPct = null,
    bool ViaScholarsOnly = false,
    string? VerdictDetailsJson = null
);
