using Meezan.Application.Common.Models;

namespace Meezan.Application.Common.Interfaces;

public interface IShariahSourceClient
{
    Task<List<ExternalStockMergedDto>> FetchMergedStocksAsync(CancellationToken cancellationToken = default);
}
