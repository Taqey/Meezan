using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypes;

public sealed record GetFundTypesQuery : IRequest<Result<List<FundTypeOptionDto>>>;
