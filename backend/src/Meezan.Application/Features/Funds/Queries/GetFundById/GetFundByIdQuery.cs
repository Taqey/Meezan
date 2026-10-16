using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundById;

public sealed record GetFundByIdQuery(int Id) : IRequest<FundDetailDto?>;
