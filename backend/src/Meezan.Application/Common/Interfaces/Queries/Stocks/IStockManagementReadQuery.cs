using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Common.Interfaces.Queries.Stocks;

public interface IStockManagementReadQuery
{
    Task<List<StockManagementItemDto>> SearchStocksAsync(string? query, CancellationToken cancellationToken = default);
    Task<List<SectorPickerItemDto>> GetAllSectorsAsync(CancellationToken cancellationToken = default);
    Task<List<IndexPickerItemDto>> GetAllIndicesAsync(CancellationToken cancellationToken = default);
}
