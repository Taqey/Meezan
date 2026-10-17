using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetFundClassificationTree;

public sealed class GetFundClassificationTreeQueryHandler : IRequestHandler<GetFundClassificationTreeQuery, Result<List<FundClassificationTreeDto>>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundClassificationTreeQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public async Task<Result<List<FundClassificationTreeDto>>> Handle(GetFundClassificationTreeQuery request, CancellationToken cancellationToken)
    {
        return await _readQuery.GetClassificationTreeAsync(cancellationToken);
    }
}
