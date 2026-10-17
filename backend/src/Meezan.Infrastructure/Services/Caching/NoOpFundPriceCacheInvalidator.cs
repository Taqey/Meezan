using Meezan.Application.Common.Interfaces.Caching;

namespace Meezan.Infrastructure.Services.Caching;

public class NoOpFundPriceCacheInvalidator : IFundPriceCacheInvalidator
{
    public Task InvalidatePriceCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
