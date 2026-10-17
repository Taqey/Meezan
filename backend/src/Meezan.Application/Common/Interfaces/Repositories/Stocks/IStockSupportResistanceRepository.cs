using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Repositories.Stocks;

public interface IStockSupportResistanceRepository
{
    Task<StockSupportResistance?> GetByStockIdAsync(int stockId, CancellationToken ct = default);
    Task UpsertAsync(StockSupportResistance sr, CancellationToken ct = default);
}
