namespace Meezan.Application.Features.Funds.Sync.Queries.GetLatestSyncRun;

using MediatR;
using Meezan.Domain.Entities;

public record GetLatestSyncRunQuery : IRequest<FundSyncLog?>;
