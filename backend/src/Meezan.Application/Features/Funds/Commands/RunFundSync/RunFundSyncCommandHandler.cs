using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.RunFundSync;

public sealed class RunFundSyncCommandHandler : IRequestHandler<RunFundSyncCommand, FundSyncLog>
{
    private readonly IFundSyncService _syncService;

    public RunFundSyncCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<FundSyncLog> Handle(RunFundSyncCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _syncService.RunSyncAsync(request.TriggeredBy, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new BadRequestException(ex.Message);
        }
    }
}
