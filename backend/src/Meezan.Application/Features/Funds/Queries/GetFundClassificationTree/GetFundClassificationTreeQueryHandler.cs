using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundClassificationTree;

public sealed class GetFundClassificationTreeQueryHandler : IRequestHandler<GetFundClassificationTreeQuery, List<FundClassificationTreeDto>>
{
    private readonly IFundReadQuery _readQuery;

    public GetFundClassificationTreeQueryHandler(IFundReadQuery readQuery)
    {
        _readQuery = readQuery;
    }

    public Task<List<FundClassificationTreeDto>> Handle(GetFundClassificationTreeQuery request, CancellationToken cancellationToken)
    {
        return _readQuery.GetClassificationTreeAsync(cancellationToken);
    }
}
