using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Listing.Dtos;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFundStats;

public sealed class GetFundStatsQueryHandler : IRequestHandler<GetFundStatsQuery, Result<FundStatsDto>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundStatsQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<FundStatsDto>> Handle(GetFundStatsQuery request, CancellationToken cancellationToken)
        => await _readQuery.GetStatsAsync(cancellationToken);
}
