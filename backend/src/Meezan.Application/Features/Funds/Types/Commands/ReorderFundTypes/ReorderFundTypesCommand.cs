using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.ReorderFundTypes;

public sealed record ReorderFundTypesCommand(List<int>? OrderedIds) : IRequest<Result<ReorderFundTypesResultDto>>, ITransactionalCommand;
