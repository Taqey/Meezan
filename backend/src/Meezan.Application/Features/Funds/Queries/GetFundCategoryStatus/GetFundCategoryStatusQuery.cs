using MediatR;

namespace Meezan.Application.Features.Funds.Queries.GetFundCategoryStatus;

public sealed record GetFundCategoryStatusQuery(int FundId) : IRequest<List<FundCategoryStatusDto>?>;
