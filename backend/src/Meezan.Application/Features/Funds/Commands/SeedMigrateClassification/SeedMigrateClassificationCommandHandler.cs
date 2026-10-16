using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Commands.SeedMigrateClassification;

public sealed class SeedMigrateClassificationCommandHandler : IRequestHandler<SeedMigrateClassificationCommand, ClassificationMigrationReport>
{
    private readonly IFundClassificationService _classification;

    public SeedMigrateClassificationCommandHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public Task<ClassificationMigrationReport> Handle(SeedMigrateClassificationCommand request, CancellationToken cancellationToken)
    {
        return _classification.EnsureSeedAndMigrateAsync("admin", cancellationToken);
    }
}
