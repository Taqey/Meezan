namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncHistory;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetSyncHistoryQuery(int Page, int PageSize) : IRequest<Result<SyncHistoryResultDto>>;
