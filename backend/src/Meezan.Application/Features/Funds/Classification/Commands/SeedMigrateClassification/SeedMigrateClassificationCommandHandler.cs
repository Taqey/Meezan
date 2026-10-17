using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Classification.Commands.SeedMigrateClassification;

public sealed class SeedMigrateClassificationCommandHandler : IRequestHandler<SeedMigrateClassificationCommand, Result<ClassificationMigrationReport>>
{
    private readonly IFundClassificationService _classification;

    public SeedMigrateClassificationCommandHandler(IFundClassificationService classification)
    {
        _classification = classification;
    }

    public async Task<Result<ClassificationMigrationReport>> Handle(SeedMigrateClassificationCommand request, CancellationToken cancellationToken)
    {
        return await _classification.EnsureSeedAndMigrateAsync("admin", cancellationToken);
    }
}
