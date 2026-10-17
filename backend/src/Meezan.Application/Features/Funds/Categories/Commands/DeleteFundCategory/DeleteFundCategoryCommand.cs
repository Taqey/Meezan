using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.DeleteFundCategory;

public sealed record DeleteFundCategoryCommand(int Id, bool Confirm = false) : IRequest<Result<DeleteFundCategoryResultDto>>, ITransactionalCommand;
