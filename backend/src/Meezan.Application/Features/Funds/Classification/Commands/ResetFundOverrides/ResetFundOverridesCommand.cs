using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Classification.Commands.ResetFundOverrides;

public sealed record ResetFundOverridesCommand(int FundId) : IRequest<Result<ResetFundOverridesResultDto>>, ITransactionalCommand;
