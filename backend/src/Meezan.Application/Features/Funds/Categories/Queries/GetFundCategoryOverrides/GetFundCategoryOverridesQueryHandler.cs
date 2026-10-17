namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryOverrides;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetFundCategoryOverridesQueryHandler : IRequestHandler<GetFundCategoryOverridesQuery, Result<List<FundCategoryOverride>>>
{
    private readonly IFundCategoryOverrideRepository _overrideRepo;

    public GetFundCategoryOverridesQueryHandler(IFundCategoryOverrideRepository overrideRepo)
    {
        _overrideRepo = overrideRepo;
    }

    public async Task<Result<List<FundCategoryOverride>>> Handle(GetFundCategoryOverridesQuery request, CancellationToken cancellationToken)
    {
        return await _overrideRepo.GetByFundIdAsync(request.FundId, cancellationToken);
    }
}
