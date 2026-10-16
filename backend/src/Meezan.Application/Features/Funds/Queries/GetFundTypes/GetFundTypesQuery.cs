using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundTypes;

public sealed record GetFundTypesQuery : IRequest<List<FundTypeOptionDto>>;
