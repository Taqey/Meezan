using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Categories.Dtos;

namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategories;

public sealed record GetFundCategoriesQuery : IRequest<Result<List<FundCategoryDto>>>;
