namespace Meezan.Application.Features.Scraping.DTOs;

/// <summary>
/// Trimmed summary DTO — only fields needed for the main stocks grid.
/// Full detail (sector names, indices with weights, shariah %, etc.)
/// is available on GET /api/stocks/{ticker}/market-data.
/// </summary>
public record StockListItemDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    List<string> Indices,
    string? ShariahStatus,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    string? Currency,
    string? SectorNameAr = null,
    decimal? PeRatio = null,
    decimal? PbRatio = null,
    decimal? Weight = null,
    string? WeightIndexCode = null,
    bool? IsCapped = null,
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
