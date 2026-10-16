using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ResetClassificationFundAssignment;

public sealed record ResetClassificationFundAssignmentCommand(int FundId) : IRequest<ResetClassificationFundAssignmentResultDto>, ITransactionalCommand;
