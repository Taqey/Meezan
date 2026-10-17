using Meezan.Application.Common.Models.Stocks;

namespace Meezan.Application.Common.Interfaces.Integrations;

public interface IShariahSourceClient
{
    Task<List<ExternalStockMergedDto>> FetchMergedStocksAsync(CancellationToken cancellationToken = default);
}
