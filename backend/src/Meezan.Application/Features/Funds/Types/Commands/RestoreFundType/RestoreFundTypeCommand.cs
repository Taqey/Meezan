using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Types.Commands.RestoreFundType;

public sealed record RestoreFundTypeCommand(int Id) : IRequest<Result<FundTypeDefinition>>, ITransactionalCommand;
