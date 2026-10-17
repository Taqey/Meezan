namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncHistory;

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

public class GetSyncHistoryQueryHandler : IRequestHandler<GetSyncHistoryQuery, Result<SyncHistoryResultDto>>
{
    private readonly IFundSyncService _syncService;

    public GetSyncHistoryQueryHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<SyncHistoryResultDto>> Handle(GetSyncHistoryQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _syncService.GetRunHistoryAsync(request.Page, request.PageSize, cancellationToken);
        return new SyncHistoryResultDto(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }
}
