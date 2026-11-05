namespace Meezan.Application.Common.Interfaces.Repositories.Stocks;

/// <summary>All optional filter/sort/page params for the stocks list query.</summary>
public record StockListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? IndexCode,
    string[]? IndexCodes,
    int? SectorId,
    string? ShariahStatus,
    string[]? ShariahStatuses,
    string? PriceComparison,
    int? MinCompliantSources,
    decimal? MinPeRatio = null,
    decimal? MaxPeRatio = null,
    decimal? MinPbRatio = null,
    decimal? MaxPbRatio = null
);
