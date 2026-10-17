using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Details.Dtos;

namespace Meezan.Application.Features.Funds.Details.Queries.GetFundById;

public sealed record GetFundByIdQuery(int Id) : IRequest<Result<FundDetailDto>>;
