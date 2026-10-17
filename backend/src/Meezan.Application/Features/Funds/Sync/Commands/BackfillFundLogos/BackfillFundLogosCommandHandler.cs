using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Sync.Commands.BackfillFundLogos;

public sealed class BackfillFundLogosCommandHandler : IRequestHandler<BackfillFundLogosCommand, Result<BackfillFundLogosResultDto>>
{
    private readonly IFundSyncService _syncService;

    public BackfillFundLogosCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<Result<BackfillFundLogosResultDto>> Handle(BackfillFundLogosCommand request, CancellationToken cancellationToken)
    {
        var stored = await _syncService.BackfillLogosAsync(cancellationToken);
        return new BackfillFundLogosResultDto(stored, $"Downloaded {stored} fund logo(s).");
    }
}
