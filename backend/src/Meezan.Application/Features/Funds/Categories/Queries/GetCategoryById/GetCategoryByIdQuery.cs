namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryById;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetCategoryByIdQuery(int Id) : IRequest<Result<FundCategory?>>;
