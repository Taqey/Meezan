using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.MergeFundTypes;

public sealed record MergeFundTypesCommand(int SourceId, int TargetId) : IRequest<Result<MergeFundTypesResultDto>>, ITransactionalCommand;
