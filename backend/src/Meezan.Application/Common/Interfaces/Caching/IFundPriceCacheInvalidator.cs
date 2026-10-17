namespace Meezan.Application.Common.Interfaces.Caching;

public interface IFundPriceCacheInvalidator
{
    Task InvalidatePriceCacheAsync(CancellationToken cancellationToken = default);
}
