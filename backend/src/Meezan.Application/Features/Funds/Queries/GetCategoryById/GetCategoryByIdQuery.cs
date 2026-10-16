namespace Meezan.Application.Features.Funds.Queries.GetCategoryById;

using MediatR;
using Meezan.Domain.Entities;

public record GetCategoryByIdQuery(int Id) : IRequest<FundCategory?>;
