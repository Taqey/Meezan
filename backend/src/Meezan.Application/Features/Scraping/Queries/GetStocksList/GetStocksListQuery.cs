using MediatR;
using Meezan.Application.Common;

namespace Meezan.Application.Features.Scraping.Queries.GetStocksList;

/// <summary>
/// Query for the paginated, filterable, sortable stocks list.
/// All params are optional; omitting them returns page 1 of 20, sorted by ticker ascending.
/// </summary>
public record GetStocksListQuery(
    // Pagination
    int? Page,
    int? PageSize,
    // Sorting — sortBy matches StockListItemDto field names (camelCase accepted)
    string? SortBy,
    string? SortDir,
    // Filters (all combinable, AND logic)
    string? Search,          // partial match on Ticker, NameAr, NameEn
    string? IndexCode,       // only constituents of this index (e.g. "EGX30")
    int? SectorId,
    string? ShariahStatus,   // "Compliant" | "NonCompliant" | "Pending" | "Blocked"
    string? PriceComparison, // "Cheap" | "Fair" | "Expensive"
    int? MinCompliantSources // 1 to 7: minimum number of ShariahSourceOpinions with Compliant status
) : IRequest<PagedResult<StockListItemDto>>;

/// <summary>
/// Trimmed summary DTO — only fields needed for the main stocks grid.
/// Full detail (sector names, indices with weights, shariah %, etc.)
/// is available on GET /api/stocks/{ticker}/market-data.
/// </summary>
public record StockListItemDto(
    string Ticker,
    string? NameAr,
    string? NameEn,
    /// <summary>Index codes this stock is a constituent of, e.g. ["EGX30","Sectoral-Indices"].</summary>
    List<string> Indices,
    /// <summary>Shariah compliance status string, null if no compliance record exists.</summary>
    string? ShariahStatus,
    decimal? ClosingPrice,
    decimal? ChangePct,
    decimal? FairValue,
    string? PriceComparison,
    decimal? FairValueDiffPct,
    string? Currency,
    string? SectorNameAr = null
);
