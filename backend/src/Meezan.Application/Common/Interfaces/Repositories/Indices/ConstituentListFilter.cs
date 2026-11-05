namespace Meezan.Application.Common.Interfaces.Repositories.Indices;

public record ConstituentListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? ShariahStatus,
    string[]? ShariahStatuses = null,
    string? PriceComparison = null,
    int? MinCompliantSources = null,
    decimal? MinPeRatio = null,
    decimal? MaxPeRatio = null,
    decimal? MinPbRatio = null,
    decimal? MaxPbRatio = null
);
