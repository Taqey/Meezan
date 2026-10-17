using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;

namespace Meezan.Application.Features.Funds.Categories.Commands.ExcludeFundFromCategory;

public sealed record ExcludeFundFromCategoryCommand(int FundId, int CategoryId) : IRequest<Result>, ITransactionalCommand;
