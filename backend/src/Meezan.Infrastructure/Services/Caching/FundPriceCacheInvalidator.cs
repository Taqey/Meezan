using Meezan.Application.Common.Interfaces.Caching;

namespace Meezan.Infrastructure.Services.Caching;

public class FundPriceCacheInvalidator : IFundPriceCacheInvalidator
{
    private readonly IFundCacheService _cacheService;

    public FundPriceCacheInvalidator(IFundCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task InvalidatePriceCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheService.InvalidatePricesAsync(cancellationToken);
    }
}
