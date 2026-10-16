namespace Meezan.Application.Features.Funds.Queries.GetLatestSyncRun;

using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

public class GetLatestSyncRunQueryHandler : IRequestHandler<GetLatestSyncRunQuery, FundSyncLog?>
{
    private readonly IFundSyncService _syncService;

    public GetLatestSyncRunQueryHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<FundSyncLog?> Handle(GetLatestSyncRunQuery request, CancellationToken cancellationToken)
    {
        return await _syncService.GetLatestRunAsync(cancellationToken);
    }
}
