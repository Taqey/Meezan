using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundPriceSync;

public sealed class RunFundPriceSyncCommandHandler : IRequestHandler<RunFundPriceSyncCommand, Result<FundPriceSyncResultDto>>
{
    private readonly IFundSyncService _syncService;

    public RunFundPriceSyncCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<FundPriceSyncResultDto>> Handle(RunFundPriceSyncCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _syncService.RunPriceSyncAsync(request.TriggeredBy, cancellationToken);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            return Error.Validation("FundPriceSyncError", ex.Message);
        }
    }
}
