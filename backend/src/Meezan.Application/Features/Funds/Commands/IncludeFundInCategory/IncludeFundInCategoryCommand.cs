using MediatR;
using Meezan.Application.Common.Interfaces;

namespace Meezan.Application.Features.Funds.Commands.IncludeFundInCategory;

public sealed record IncludeFundInCategoryCommand(int FundId, int CategoryId) : IRequest<Unit>, ITransactionalCommand;
