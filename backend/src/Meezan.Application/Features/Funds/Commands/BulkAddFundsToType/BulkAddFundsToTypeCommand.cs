using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.BulkAddFundsToType;

public sealed record BulkAddFundsToTypeCommand(int TypeId, List<int>? FundIds) : IRequest<BulkAddFundsToTypeResultDto>, ITransactionalCommand;
