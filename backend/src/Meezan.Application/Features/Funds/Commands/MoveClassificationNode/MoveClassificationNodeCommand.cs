using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.MoveClassificationNode;

public sealed record MoveClassificationNodeCommand(int Id, int? NewParentId) : IRequest<AdminClassificationNodeDto>, ITransactionalCommand;
