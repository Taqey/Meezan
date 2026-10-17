using MediatR;
using Meezan.Application.Common.Models.Caching;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Queries.GetCacheStatus;

public sealed record GetCacheStatusQuery : IRequest<Result<CacheStatusDto>>;
