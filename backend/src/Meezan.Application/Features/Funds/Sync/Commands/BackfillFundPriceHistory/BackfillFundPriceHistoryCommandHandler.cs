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

public sealed class FetchFundMissingPricesCommandHandler
    : IRequestHandler<FetchFundMissingPricesCommand, Result<FetchFundMissingPricesResultDto>>
{
    private readonly IFundSyncService _syncService;

    public FetchFundMissingPricesCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<FetchFundMissingPricesResultDto>> Handle(
        FetchFundMissingPricesCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _syncService.FetchMissingPricesForFundAsync(
                request.FundId,
                request.TriggeredBy,
                request.BypassCooldown,
                cancellationToken);

            if (result.AlreadyRunning)
            {
                return Error.Conflict("FundMissingPricesAlreadyRunning", result.Message, result);
            }

            if (result.OnCooldown)
            {
                return Error.Conflict("FundMissingPricesCooldown", result.Message, result);
            }

            return result;
        }
        catch (KeyNotFoundException ex)
        {
            return Error.NotFound("FundNotFound", ex.Message);
        }
        catch (Exception ex)
        {
            return Error.Validation("FetchFundMissingPricesError", ex.Message);
        }
    }
}


