using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.ResetCategoryOverrides;

public sealed record ResetCategoryOverridesCommand(int CategoryId) : IRequest<Result<ResetCategoryOverridesResultDto>>, ITransactionalCommand;
