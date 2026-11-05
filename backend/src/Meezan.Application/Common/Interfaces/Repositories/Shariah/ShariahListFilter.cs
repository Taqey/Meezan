namespace Meezan.Application.Common.Interfaces.Repositories.Shariah;

public record ShariahListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? Status
);
