namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryChanges;

using MediatR;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;
using Meezan.Domain.Entities;

public record GetCategoryChangesQuery(
    int? FundId,
    int? CategoryId,
    CategoryChangeAction? Action,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page,
    int PageSize) : IRequest<CategoryChangesResultDto>;

public record CategoryChangesResultDto(
    List<FundCategoryChangeLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
