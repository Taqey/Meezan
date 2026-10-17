using MediatR;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Sync.Commands.RunFundSync;

public sealed record RunFundSyncCommand(string TriggeredBy = "Manual") : IRequest<Result<FundSyncLog>>;
