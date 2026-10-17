using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.GetSectorPickers;

public sealed class GetSectorPickersQueryHandler : IRequestHandler<GetSectorPickersQuery, Result<List<SectorPickerItemDto>>>
{
    private readonly IStockManagementReadQuery _readQuery;

    public GetSectorPickersQueryHandler(IStockManagementReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<SectorPickerItemDto>>> Handle(GetSectorPickersQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetAllSectorsAsync(cancellationToken);
    }
}
