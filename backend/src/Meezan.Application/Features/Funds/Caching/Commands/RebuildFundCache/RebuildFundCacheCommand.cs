using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Caching.Commands.RebuildFundCache;

public sealed record RebuildFundCacheCommand : IRequest<Result<string>>;
