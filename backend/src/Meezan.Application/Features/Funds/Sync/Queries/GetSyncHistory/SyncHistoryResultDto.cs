namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncHistory;

using Meezan.Domain.Entities;

public record SyncHistoryResultDto(
    List<FundSyncLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
