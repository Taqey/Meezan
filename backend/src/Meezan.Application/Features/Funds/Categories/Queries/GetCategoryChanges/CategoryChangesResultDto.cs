namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryChanges;

using Meezan.Domain.Entities;

public record CategoryChangesResultDto(
    List<FundCategoryChangeLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
