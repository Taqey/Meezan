using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.BulkRemoveClassificationFunds;

public sealed record BulkRemoveClassificationFundsCommand(
    int NodeId,
    List<int>? FundIds,
    int? DestinationNodeId,
    bool ResetToAuto) : IRequest<BulkRemoveFundsResultDto>, ITransactionalCommand;
