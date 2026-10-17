using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Models.Caching;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Queries.GetCacheStatus;

public sealed class GetCacheStatusQueryHandler : IRequestHandler<GetCacheStatusQuery, Result<CacheStatusDto>>
{
    private readonly IFundCacheService _cacheService;

    public GetCacheStatusQueryHandler(IFundCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<Result<CacheStatusDto>> Handle(GetCacheStatusQuery request, CancellationToken cancellationToken)
    {
        var status = await _cacheService.GetStatusAsync(cancellationToken);
        return Result<CacheStatusDto>.Success(status);
    }
}
