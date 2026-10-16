using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.MergeClassificationNodes;

public sealed record MergeClassificationNodesCommand(int SourceId, int TargetId) : IRequest<MergeClassificationNodesResultDto>, ITransactionalCommand;
