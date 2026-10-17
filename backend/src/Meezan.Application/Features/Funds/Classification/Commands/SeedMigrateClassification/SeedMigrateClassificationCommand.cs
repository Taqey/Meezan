using MediatR;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Results;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Classification.Commands.SeedMigrateClassification;

public sealed record SeedMigrateClassificationCommand : IRequest<Result<ClassificationMigrationReport>>, ITransactionalCommand;
