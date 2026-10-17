using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Commands.ClearFundCache;

public sealed record ClearFundCacheCommand : IRequest<Result<string>>;
