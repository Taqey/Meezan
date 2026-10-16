using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Commands.SeedMigrateClassification;

public sealed record SeedMigrateClassificationCommand : IRequest<ClassificationMigrationReport>, ITransactionalCommand;
