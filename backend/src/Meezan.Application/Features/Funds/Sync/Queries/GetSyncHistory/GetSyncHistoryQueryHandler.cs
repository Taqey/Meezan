namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncHistory;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;

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
