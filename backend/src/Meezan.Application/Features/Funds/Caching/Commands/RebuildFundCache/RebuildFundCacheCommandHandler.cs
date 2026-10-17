using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Commands.RebuildFundCache;

public sealed class RebuildFundCacheCommandHandler : IRequestHandler<RebuildFundCacheCommand, Result<string>>
{
    private readonly IFundCacheService _cacheService;

    public RebuildFundCacheCommandHandler(IFundCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<string>> Handle(RebuildFundCacheCommand request, CancellationToken cancellationToken)
    {
        await _cacheService.RebuildAllAsync(cancellationToken);
        return Result<string>.Success("Fund cache rebuilt successfully.");
    }
}
