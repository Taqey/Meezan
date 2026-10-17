namespace Meezan.Application.Features.Funds.Categories.Queries.GetAllCategories;

using MediatR;
using Meezan.Domain.Entities;

public record GetAllCategoriesQuery(bool IncludeDeleted) : IRequest<List<FundCategory>>;
