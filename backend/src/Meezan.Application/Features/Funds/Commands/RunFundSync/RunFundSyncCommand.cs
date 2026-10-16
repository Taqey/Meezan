using MediatR;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.RunFundSync;

public sealed record RunFundSyncCommand(string TriggeredBy = "Manual") : IRequest<FundSyncLog>;
