using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Features.Funds.Sync.Commands.BackfillFundPriceHistory;

public sealed class BackfillFundPriceHistoryCommandHandler
    : IRequestHandler<BackfillFundPriceHistoryCommand, Result<FundPriceHistoryBackfillResultDto>>
{
    private readonly IFundSyncService _syncService;

    public BackfillFundPriceHistoryCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<FundPriceHistoryBackfillResultDto>> Handle(
        BackfillFundPriceHistoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _syncService.BackfillPriceHistoryAsync(request.DryRun, request.SpecificFundId, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            return Error.Validation("FundPriceBackfillError", ex.Message);
        }
    }
}
