using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.RestoreFundCategory;

public sealed record RestoreFundCategoryCommand(int Id) : IRequest<FundCategory>, ITransactionalCommand;
