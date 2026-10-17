using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.SetFundType;

public sealed record SetFundTypeCommand(int FundId, int TypeId) : IRequest<Result<SetFundTypeResultDto>>, ITransactionalCommand;
