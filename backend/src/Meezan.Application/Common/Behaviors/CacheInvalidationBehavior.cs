using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Common.Behaviors;

public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IFundCacheService _cacheService;

    public CacheInvalidationBehavior(IFundCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        // Only invalidate if the command succeeded
        if (response is IResult result && result.IsFailure)
        {
            return response;
        }

        if (request is IInvalidatesFundCatalog)
        {
            await _cacheService.InvalidateCatalogAsync(cancellationToken);
        }

        if (request is IInvalidatesFundPrices)
        {
            await _cacheService.InvalidatePricesAsync(cancellationToken);
        }

        return response;
    }
}
