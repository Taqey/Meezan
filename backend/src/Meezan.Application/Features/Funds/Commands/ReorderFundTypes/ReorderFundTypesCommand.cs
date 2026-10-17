using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ReorderFundTypes;

public sealed record ReorderFundTypesCommand(List<int>? OrderedIds) : IRequest<ReorderFundTypesResultDto>, ITransactionalCommand;
