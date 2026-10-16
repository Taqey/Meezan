using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.BackfillFundLogos;

public sealed class BackfillFundLogosCommandHandler : IRequestHandler<BackfillFundLogosCommand, BackfillFundLogosResultDto>
{
    private readonly IFundSyncService _syncService;

    public BackfillFundLogosCommandHandler(IFundSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<BackfillFundLogosResultDto> Handle(BackfillFundLogosCommand request, CancellationToken cancellationToken)
    {
        var stored = await _syncService.BackfillLogosAsync(cancellationToken);
        return new BackfillFundLogosResultDto(stored, $"Downloaded {stored} fund logo(s).");
    }
}
