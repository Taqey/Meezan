using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
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
