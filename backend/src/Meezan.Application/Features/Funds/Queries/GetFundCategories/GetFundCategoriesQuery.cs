using MediatR;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetFundCategories;

public sealed record GetFundCategoriesQuery : IRequest<List<FundCategoryDto>>;
