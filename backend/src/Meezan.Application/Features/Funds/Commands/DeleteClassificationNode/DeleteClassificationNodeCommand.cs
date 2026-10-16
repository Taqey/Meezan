using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.DeleteClassificationNode;

public sealed record DeleteClassificationNodeCommand(
    int Id,
    bool Confirm = false,
    int? MoveFundsToNodeId = null,
    bool ResetFundsToAuto = false,
    int? MoveChildrenToNodeId = null) : IRequest<DeleteClassificationNodeResultDto>, ITransactionalCommand;
