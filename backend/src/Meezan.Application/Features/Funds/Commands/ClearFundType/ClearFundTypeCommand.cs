using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ClearFundType;

public sealed record ClearFundTypeCommand(int FundId) : IRequest<ClearFundTypeResultDto>, ITransactionalCommand;
