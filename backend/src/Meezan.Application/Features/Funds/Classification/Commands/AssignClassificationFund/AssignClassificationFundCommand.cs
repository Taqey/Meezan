using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Commands.AssignClassificationFund;

public sealed record AssignClassificationFundCommand(int FundId, int NodeId, string? Reason) : IRequest<Result<AssignClassificationFundResultDto>>, ITransactionalCommand;
