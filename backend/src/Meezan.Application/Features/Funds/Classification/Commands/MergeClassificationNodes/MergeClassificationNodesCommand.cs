using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Commands.MergeClassificationNodes;

public sealed record MergeClassificationNodesCommand(int SourceId, int TargetId) : IRequest<Result<MergeClassificationNodesResultDto>>, ITransactionalCommand;
