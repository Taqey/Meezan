using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Listing.Dtos;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFundAssetManagers;

public sealed class GetFundAssetManagersQueryHandler : IRequestHandler<GetFundAssetManagersQuery, Result<List<FundAssetManagerOptionDto>>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundAssetManagersQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<FundAssetManagerOptionDto>>> Handle(GetFundAssetManagersQuery request, CancellationToken cancellationToken)
        => await _readQuery.GetAssetManagersAsync(cancellationToken);
}
