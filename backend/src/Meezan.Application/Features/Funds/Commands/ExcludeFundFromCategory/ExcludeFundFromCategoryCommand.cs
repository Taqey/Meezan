using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.ExcludeFundFromCategory;

public sealed record ExcludeFundFromCategoryCommand(int FundId, int CategoryId) : IRequest<Unit>, ITransactionalCommand;
