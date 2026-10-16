using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.AssignClassificationFund;

public sealed record AssignClassificationFundCommand(int FundId, int NodeId, string? Reason) : IRequest<AssignClassificationFundResultDto>, ITransactionalCommand;
