namespace Meezan.Application.Features.Funds.Sync.Queries.GetLatestSyncRun;

using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public class GetLatestSyncRunQueryHandler : IRequestHandler<GetLatestSyncRunQuery, Result<FundSyncLog?>>
{
    private readonly IFundSyncService _syncService;

    public GetLatestSyncRunQueryHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<FundSyncLog?>> Handle(GetLatestSyncRunQuery request, CancellationToken cancellationToken)
    {
        return await _syncService.GetLatestRunAsync(cancellationToken);
    }
}
