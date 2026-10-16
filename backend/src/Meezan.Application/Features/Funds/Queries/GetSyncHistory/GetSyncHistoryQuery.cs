namespace Meezan.Application.Features.Funds.Queries.GetSyncHistory;

using MediatR;
using Meezan.Domain.Entities;

public record GetSyncHistoryQuery(int Page, int PageSize) : IRequest<SyncHistoryResultDto>;

public record SyncHistoryResultDto(
    List<FundSyncLog> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
