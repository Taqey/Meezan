using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

public interface IStockMarketDataRepository
{
    Task<StockMarketData?> GetByStockIdAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(StockMarketData data, CancellationToken ct = default);
}
