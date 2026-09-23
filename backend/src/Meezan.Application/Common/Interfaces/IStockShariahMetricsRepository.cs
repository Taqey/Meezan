using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IStockShariahMetricsRepository
{
    Task<StockShariahMetrics?> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default);
    Task AddAsync(StockShariahMetrics metrics, CancellationToken cancellationToken = default);
    void Update(StockShariahMetrics metrics);
}
