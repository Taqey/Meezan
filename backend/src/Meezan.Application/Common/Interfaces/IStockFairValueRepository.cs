using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IStockFairValueRepository
{
    Task<StockFairValue?> GetByStockIdAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(StockFairValue fairValue, IEnumerable<StockFairValueMethod> methods, CancellationToken ct = default);
}
