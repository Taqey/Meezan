using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeById;

public sealed record GetFundTypeByIdQuery(int Id) : IRequest<Result<FundTypeDefinition>>;
