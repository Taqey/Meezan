using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ResetCategoryOverrides;

public sealed record ResetCategoryOverridesCommand(int CategoryId) : IRequest<ResetCategoryOverridesResultDto>, ITransactionalCommand;
