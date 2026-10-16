using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.DeleteFundCategory;

public sealed record DeleteFundCategoryCommand(int Id, bool Confirm = false) : IRequest<DeleteFundCategoryResultDto>, ITransactionalCommand;
