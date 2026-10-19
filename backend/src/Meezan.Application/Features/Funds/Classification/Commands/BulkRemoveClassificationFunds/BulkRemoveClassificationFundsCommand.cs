using Meezan.Application.Common.Interfaces.Caching;
using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.BulkRemoveClassificationFunds;

public sealed record BulkRemoveClassificationFundsCommand(
    int NodeId,
    List<int>? FundIds,
    int? DestinationNodeId,
    bool ResetToAuto) : IRequest<Result<BulkRemoveFundsResultDto>>, ITransactionalCommand, IInvalidatesFundCatalog;
