namespace Meezan.Application.Features.Funds.Queries.GetCategoryChanges;

using MediatR;
using Meezan.Application.Common.Models;
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
