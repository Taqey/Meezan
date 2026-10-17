using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Types.Commands.ClearFundType;

public sealed record ClearFundTypeCommand(int FundId) : IRequest<Result<ClearFundTypeResultDto>>, ITransactionalCommand;
