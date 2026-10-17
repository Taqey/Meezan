using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.DeleteFundType;

public sealed record DeleteFundTypeCommand(int Id, bool Confirm = false) : IRequest<Result<DeleteFundTypeResultDto>>, ITransactionalCommand;
