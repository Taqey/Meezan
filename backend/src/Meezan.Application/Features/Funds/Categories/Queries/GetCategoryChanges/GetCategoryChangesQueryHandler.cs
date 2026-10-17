namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryChanges;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Results;

public class GetCategoryChangesQueryHandler : IRequestHandler<GetCategoryChangesQuery, Result<CategoryChangesResultDto>>
{
    private readonly IFundCategoryChangeLogRepository _changeLogRepo;

    public GetCategoryChangesQueryHandler(IFundCategoryChangeLogRepository changeLogRepo)
    {
        _changeLogRepo = changeLogRepo;
    }

    public async Task<Result<CategoryChangesResultDto>> Handle(GetCategoryChangesQuery request, CancellationToken cancellationToken)
    {
        var filter = new FundCategoryChangeLogFilter
        {
            FundId = request.FundId,
            CategoryId = request.CategoryId,
            Action = request.Action,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Page = request.Page,
            PageSize = request.PageSize
        };
        var (items, totalCount) = await _changeLogRepo.QueryPagedAsync(filter, cancellationToken);
        return new CategoryChangesResultDto(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }
}
