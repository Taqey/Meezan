using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Classification.Commands.ReclassifyUnclassified;

public sealed record ReclassifyUnclassifiedCommand : IRequest<Result<ReclassifyResult>>, ITransactionalCommand;
