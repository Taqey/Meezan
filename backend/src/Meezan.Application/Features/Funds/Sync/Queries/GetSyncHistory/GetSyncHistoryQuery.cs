namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncHistory;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetSyncHistoryQuery(int Page, int PageSize) : IRequest<Result<SyncHistoryResultDto>>;

public record SyncHistoryResultDto(
    List<FundSyncLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
