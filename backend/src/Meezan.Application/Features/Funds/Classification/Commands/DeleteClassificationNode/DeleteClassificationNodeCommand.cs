using Meezan.Application.Common.Interfaces.Caching;
using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Commands.DeleteClassificationNode;

public sealed record DeleteClassificationNodeCommand(
    int Id,
    bool Confirm = false,
    int? MoveFundsToNodeId = null,
    bool ResetFundsToAuto = false,
    int? MoveChildrenToNodeId = null) : IRequest<Result<DeleteClassificationNodeResultDto>>, ITransactionalCommand, IInvalidatesFundCatalog;
