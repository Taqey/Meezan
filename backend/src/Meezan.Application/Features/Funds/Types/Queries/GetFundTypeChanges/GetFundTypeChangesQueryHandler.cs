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

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeChanges;

public sealed class GetFundTypeChangesQueryHandler : IRequestHandler<GetFundTypeChangesQuery, Result<FundTypeChangesResultDto>>
{
    private readonly IFundTypeChangeLogRepository _changeLogRepo;

    public GetFundTypeChangesQueryHandler(IFundTypeChangeLogRepository changeLogRepo)
    {
        _changeLogRepo = changeLogRepo;
    }

    public async Task<Result<FundTypeChangesResultDto>> Handle(GetFundTypeChangesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _changeLogRepo.QueryPagedAsync(request.Page, request.PageSize, cancellationToken);
        return new FundTypeChangesResultDto(
            items,
            total,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling(total / (double)request.PageSize)
        );
    }
}
