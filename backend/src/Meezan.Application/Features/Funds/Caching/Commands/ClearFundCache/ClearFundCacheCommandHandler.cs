using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Commands.ClearFundCache;

public sealed class ClearFundCacheCommandHandler : IRequestHandler<ClearFundCacheCommand, Result<string>>
{
    private readonly IFundCacheService _cacheService;

    public ClearFundCacheCommandHandler(IFundCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<string>> Handle(ClearFundCacheCommand request, CancellationToken cancellationToken)
    {
        await _cacheService.ClearFundCacheAsync(cancellationToken);
        return Result<string>.Success("Fund cache cleared successfully for application prefix.");
    }
}
