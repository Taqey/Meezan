using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.BulkAddFundsToType;

public sealed record BulkAddFundsToTypeCommand(int TypeId, List<int>? FundIds) : IRequest<Result<BulkAddFundsToTypeResultDto>>, ITransactionalCommand;
