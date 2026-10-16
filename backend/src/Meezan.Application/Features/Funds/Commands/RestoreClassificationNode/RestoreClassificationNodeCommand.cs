using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.RestoreClassificationNode;

public sealed record RestoreClassificationNodeCommand(int Id) : IRequest<AdminClassificationNodeDto>, ITransactionalCommand;
