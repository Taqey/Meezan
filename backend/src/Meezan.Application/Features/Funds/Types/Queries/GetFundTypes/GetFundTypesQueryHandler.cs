using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypes;

public sealed class GetFundTypesQueryHandler : IRequestHandler<GetFundTypesQuery, Result<List<FundTypeOptionDto>>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundTypesQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<FundTypeOptionDto>>> Handle(GetFundTypesQuery request, CancellationToken cancellationToken)
        => await _readQuery.GetTypeOptionsAsync(cancellationToken);
}
