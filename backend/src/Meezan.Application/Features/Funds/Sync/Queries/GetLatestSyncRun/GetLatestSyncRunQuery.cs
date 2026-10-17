namespace Meezan.Application.Features.Funds.Sync.Queries.GetLatestSyncRun;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetLatestSyncRunQuery : IRequest<Result<FundSyncLog?>>;
