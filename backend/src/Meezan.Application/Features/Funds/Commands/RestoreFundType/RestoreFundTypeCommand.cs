using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Commands.RestoreFundType;

public sealed record RestoreFundTypeCommand(int Id) : IRequest<FundTypeDefinition>, ITransactionalCommand;
