using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Listing.Dtos;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFundStats;

public sealed record GetFundStatsQuery : IRequest<Result<FundStatsDto>>;
