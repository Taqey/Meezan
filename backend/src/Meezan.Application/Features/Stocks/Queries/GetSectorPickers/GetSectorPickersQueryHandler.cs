using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Stocks.DTOs;

namespace Meezan.Application.Features.Stocks.Queries.GetSectorPickers;

public sealed class GetSectorPickersQueryHandler : IRequestHandler<GetSectorPickersQuery, List<SectorPickerItemDto>>
{
    private readonly IStockManagementReadQuery _readQuery;

    public GetSectorPickersQueryHandler(IStockManagementReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<List<SectorPickerItemDto>> Handle(GetSectorPickersQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetAllSectorsAsync(cancellationToken);
    }
}
