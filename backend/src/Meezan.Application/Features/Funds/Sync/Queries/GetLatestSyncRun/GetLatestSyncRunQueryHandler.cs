namespace Meezan.Application.Features.Funds.Sync.Queries.GetLatestSyncRun;

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
