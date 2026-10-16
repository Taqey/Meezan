namespace Meezan.Application.Features.Funds.Queries.GetLatestSyncRun;

using MediatR;
using Meezan.Domain.Entities;

public record GetLatestSyncRunQuery : IRequest<FundSyncLog?>;
