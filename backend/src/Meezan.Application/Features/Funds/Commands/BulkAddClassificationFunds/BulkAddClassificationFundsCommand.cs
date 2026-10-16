using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.BulkAddClassificationFunds;

public sealed record BulkAddClassificationFundsCommand(int NodeId, List<int>? FundIds) : IRequest<BulkAddFundsResultDto>, ITransactionalCommand;
