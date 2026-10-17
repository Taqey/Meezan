using MediatR;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Queries.GetFundCategoryStatus;

public sealed record GetFundCategoryStatusQuery(int FundId) : IRequest<Result<List<FundCategoryStatusDto>?>>;
