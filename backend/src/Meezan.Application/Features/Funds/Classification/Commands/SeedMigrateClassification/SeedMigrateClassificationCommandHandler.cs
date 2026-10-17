using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Classification.Commands.SeedMigrateClassification;

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
