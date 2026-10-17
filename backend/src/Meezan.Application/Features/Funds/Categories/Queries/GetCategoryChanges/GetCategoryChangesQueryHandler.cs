namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryChanges;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;

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
