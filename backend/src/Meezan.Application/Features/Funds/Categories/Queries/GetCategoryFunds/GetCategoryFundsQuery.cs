namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryFunds;

using MediatR;
using Meezan.Application.Common.Results;

public record GetCategoryFundsQuery(
    int CategoryId,
    string? Search,
    string? Source,
    int Page,
    int PageSize) : IRequest<Result<CategoryFundsResultDto?>>;

