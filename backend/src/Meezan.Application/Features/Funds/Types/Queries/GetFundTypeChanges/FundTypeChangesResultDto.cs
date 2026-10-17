using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Queries.GetFundTypeChanges;

public sealed record FundTypeChangesResultDto(
    List<FundTypeChangeLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
