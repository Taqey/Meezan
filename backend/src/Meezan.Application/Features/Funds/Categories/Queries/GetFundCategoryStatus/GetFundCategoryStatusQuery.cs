using MediatR;

namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryStatus;

public sealed record GetFundCategoryStatusQuery(int FundId) : IRequest<List<FundCategoryStatusDto>?>;
