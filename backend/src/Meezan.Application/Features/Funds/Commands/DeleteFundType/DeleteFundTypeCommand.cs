using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.DeleteFundType;

public sealed record DeleteFundTypeCommand(int Id, bool Confirm = false) : IRequest<DeleteFundTypeResultDto>, ITransactionalCommand;
