using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.SearchStocks;

public sealed class SearchStocksQueryHandler : IRequestHandler<SearchStocksQuery, List<StockManagementItemDto>>
{
    private readonly IStockManagementReadQuery _readQuery;

    public SearchStocksQueryHandler(IStockManagementReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<List<StockManagementItemDto>> Handle(SearchStocksQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.SearchStocksAsync(request.Query, cancellationToken);
    }
}
