using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.MoveFundType;

public sealed record MoveFundTypeCommand(int FundId, int? TargetTypeId) : IRequest<MoveFundTypeResultDto>, ITransactionalCommand;
