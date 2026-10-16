using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundStats;

public sealed record GetFundStatsQuery : IRequest<FundStatsDto>;
