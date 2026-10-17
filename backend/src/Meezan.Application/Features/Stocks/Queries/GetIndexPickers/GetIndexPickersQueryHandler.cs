using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.GetIndexPickers;

public sealed class GetIndexPickersQueryHandler : IRequestHandler<GetIndexPickersQuery, Result<List<IndexPickerItemDto>>>
{
    private readonly IStockManagementReadQuery _readQuery;

    public GetIndexPickersQueryHandler(IStockManagementReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<IndexPickerItemDto>>> Handle(GetIndexPickersQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetAllIndicesAsync(cancellationToken);
    }
}
