using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IShariahComplianceRepository
{
    Task<ShariahCompliance?> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task<ShariahCompliance?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<ShariahCompliance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(List<ShariahCompliance> Items, int TotalCount)> QueryPagedAsync(
        ShariahListFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(ShariahCompliance compliance, CancellationToken cancellationToken = default);
    void Update(ShariahCompliance compliance);
}

public record ShariahListFilter(
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir,
    string? Search,
    string? Status
);
