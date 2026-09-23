namespace Meezan.Application.Common;

/// <summary>
/// Uniform paged envelope returned by every paginated list endpoint.
/// Shape: { items, page, pageSize, totalCount, totalPages }
/// </summary>
public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
)
{
    public static PagedResult<T> Create(List<T> items, int page, int pageSize, int totalCount) =>
        new(items, page, pageSize, totalCount, (int)Math.Ceiling(totalCount / (double)pageSize));
}
