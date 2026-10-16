using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypes;

public sealed class GetFundTypesQueryHandler : IRequestHandler<GetFundTypesQuery, List<FundTypeOptionDto>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundTypesQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public Task<List<FundTypeOptionDto>> Handle(GetFundTypesQuery request, CancellationToken cancellationToken)
        => _readQuery.GetTypeOptionsAsync(cancellationToken);
}
