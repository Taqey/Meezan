namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryOverrides;

using MediatR;
using Meezan.Domain.Entities;

public record GetFundCategoryOverridesQuery(int FundId) : IRequest<List<FundCategoryOverride>>;
