namespace Meezan.Application.Features.Funds.Queries.GetFundCategoryOverrides;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetFundCategoryOverridesQueryHandler : IRequestHandler<GetFundCategoryOverridesQuery, List<FundCategoryOverride>>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;

    public GetFundCategoryOverridesQueryHandler(IFundCategoryOverrideRepository overrideRepo)
    {
        _overrideRepo = overrideRepo;
    }

    public async Task<List<FundCategoryOverride>> Handle(GetFundCategoryOverridesQuery request, CancellationToken cancellationToken)
    {
        return await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
    }
}
