using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;

namespace Meezan.Application.Features.Funds.Sync.Queries.GetFundSyncStatus;

public sealed record GetFundSyncStatusQuery : IRequest<Result<FundSyncStatusDto>>;
