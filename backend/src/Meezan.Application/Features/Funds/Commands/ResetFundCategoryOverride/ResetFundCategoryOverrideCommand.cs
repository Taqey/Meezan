using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ResetFundCategoryOverride;

public sealed record ResetFundCategoryOverrideCommand(int FundId, int CategoryId) : IRequest<Unit>, ITransactionalCommand;
