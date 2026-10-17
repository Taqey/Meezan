using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.SetFundType;

public sealed record SetFundTypeCommand(int FundId, int TypeId) : IRequest<SetFundTypeResultDto>, ITransactionalCommand;
