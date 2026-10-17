using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.MoveFundType;

public sealed record MoveFundTypeCommand(int FundId, int? TargetTypeId) : IRequest<Result<MoveFundTypeResultDto>>, ITransactionalCommand;
