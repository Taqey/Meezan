using MediatR;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;

public sealed record ResetClassificationFundAssignmentCommand(int FundId) : IRequest<Result<ResetClassificationFundAssignmentResultDto>>, ITransactionalCommand, IInvalidatesFundCatalog;
