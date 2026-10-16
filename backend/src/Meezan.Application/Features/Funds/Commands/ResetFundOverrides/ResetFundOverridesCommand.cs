using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ResetFundOverrides;

public sealed record ResetFundOverridesCommand(int FundId) : IRequest<ResetFundOverridesResultDto>, ITransactionalCommand;
