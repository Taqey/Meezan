using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.SearchStocks;

public sealed class SearchStocksQueryHandler : IRequestHandler<SearchStocksQuery, Result<List<StockManagementItemDto>>>
{
    private readonly IStockManagementReadQuery _readQuery;

    public SearchStocksQueryHandler(IStockManagementReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<StockManagementItemDto>>> Handle(SearchStocksQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.SearchStocksAsync(request.Query, cancellationToken);
    }
}
