using Meezan.Application.Common.Interfaces.Caching;
using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.RestoreClassificationNode;

public sealed record RestoreClassificationNodeCommand(int Id) : IRequest<Result<AdminClassificationNodeDto>>, ITransactionalCommand, IInvalidatesFundCatalog;
