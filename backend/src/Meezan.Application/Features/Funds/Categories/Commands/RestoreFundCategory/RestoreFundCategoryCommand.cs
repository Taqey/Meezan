using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Categories.Commands.RestoreFundCategory;

public sealed record RestoreFundCategoryCommand(int Id) : IRequest<Result<FundCategory>>, ITransactionalCommand;
