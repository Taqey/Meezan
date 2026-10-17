using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.MergeFundTypes;

public sealed record MergeFundTypesCommand(int SourceId, int TargetId) : IRequest<MergeFundTypesResultDto>, ITransactionalCommand;
