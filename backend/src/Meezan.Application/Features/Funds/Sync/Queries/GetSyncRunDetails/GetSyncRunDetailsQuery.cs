namespace Meezan.Application.Features.Funds.Sync.Queries.GetSyncRunDetails;

using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

public record GetSyncRunDetailsQuery(int LogId) : IRequest<Result<List<FundSyncLogDetail>>>;
