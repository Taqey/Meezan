using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Commands.ReclassifyUnclassified;

public sealed record ReclassifyUnclassifiedCommand : IRequest<ReclassifyResult>, ITransactionalCommand;
