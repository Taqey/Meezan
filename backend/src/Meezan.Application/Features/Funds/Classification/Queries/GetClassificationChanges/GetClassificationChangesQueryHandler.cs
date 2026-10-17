using Meezan.Application.Common.Results;
using MediatR;
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

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationChanges;

public sealed class GetClassificationChangesQueryHandler : IRequestHandler<GetClassificationChangesQuery, Result<ClassificationChangesResultDto>>
{
    private readonly IFundClassificationChangeLogRepository _repo;

    public GetClassificationChangesQueryHandler(IFundClassificationChangeLogRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<ClassificationChangesResultDto>> Handle(GetClassificationChangesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repo.QueryPagedAsync(
            request.FundId,
            request.NodeId,
            request.Action,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new ClassificationChangesResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}
