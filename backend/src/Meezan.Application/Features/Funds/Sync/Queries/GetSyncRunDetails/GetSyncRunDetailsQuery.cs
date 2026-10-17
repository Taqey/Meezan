namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncRunDetails;

using MediatR;
using Meezan.Domain.Entities;

public record GetSyncRunDetailsQuery(int LogId) : IRequest<List<FundSyncLogDetail>>;
