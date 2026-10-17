using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.ResetFundCategoryOverride;

public sealed record ResetFundCategoryOverrideCommand(int FundId, int CategoryId) : IRequest<Result>, ITransactionalCommand;
