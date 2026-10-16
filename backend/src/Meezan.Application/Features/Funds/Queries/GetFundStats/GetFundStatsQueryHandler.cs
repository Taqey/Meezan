using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundStats;

public sealed class GetFundStatsQueryHandler : IRequestHandler<GetFundStatsQuery, FundStatsDto>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundStatsQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public Task<FundStatsDto> Handle(GetFundStatsQuery request, CancellationToken cancellationToken)
        => _readQuery.GetStatsAsync(cancellationToken);
}
