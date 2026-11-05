namespace Meezan.Application.Features.Funds.Listing.Queries.GetAdminFunds;

using Meezan.Domain.Entities;

public record AdminFundsResultDto(
    List<Fund> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
