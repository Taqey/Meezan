namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryChanges;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetCategoryChangesQuery(
    int? FundId,
    int? CategoryId,
    CategoryChangeAction? Action,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page,
    int PageSize) : IRequest<Result<CategoryChangesResultDto>>;
