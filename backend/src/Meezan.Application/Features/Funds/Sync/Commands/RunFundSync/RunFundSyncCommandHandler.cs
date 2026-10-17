using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Common.Exceptions;
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

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundSync;

public sealed class RunFundSyncCommandHandler : IRequestHandler<RunFundSyncCommand, Result<FundSyncLog>>
{
    private readonly IFundSyncService _syncService;

    public RunFundSyncCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<FundSyncLog>> Handle(RunFundSyncCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _syncService.RunSyncAsync(request.TriggeredBy, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("FundSyncError", ex.Message);
        }
    }
}
