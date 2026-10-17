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
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationBackup;

public sealed class GetClassificationBackupQueryHandler : IRequestHandler<GetClassificationBackupQuery, List<FundClassificationBackup>>
{
    private readonly IFundClassificationBackupRepository _repo;

    public GetClassificationBackupQueryHandler(IFundClassificationBackupRepository repo)
    {
        _repo = repo;
    }

    public Task<List<FundClassificationBackup>> Handle(GetClassificationBackupQuery request, CancellationToken cancellationToken)
    {
        return _repo.GetAllAsync(cancellationToken);
    }
}
