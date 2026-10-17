namespace Meezan.Application.Features.Funds.Categories.Queries.GetAllCategories;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetAllCategoriesQuery(bool IncludeDeleted) : IRequest<Result<List<FundCategory>>>;
