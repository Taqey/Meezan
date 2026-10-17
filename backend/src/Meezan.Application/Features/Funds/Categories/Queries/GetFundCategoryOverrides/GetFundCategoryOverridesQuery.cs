namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryOverrides;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetFundCategoryOverridesQuery(int FundId) : IRequest<Result<List<FundCategoryOverride>>>;
