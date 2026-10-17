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

namespace Meezan.Infrastructure.Tests.Testing;

public class FakeMubasherMarketSnapshotClient : IMubasherMarketSnapshotClient
{
    public Task<MarketSnapshotQuote?> ScrapeIndexSummaryAsync(string mubasherSlug, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MarketSnapshotQuote?>(new MarketSnapshotQuote(
            Value: 30500.5m,
            Change: 150.2m,
            ChangePct: 0.5m,
            IsUp: true,
            SourceDateText: "17/10/2026 14:30:00"
        ));
    }
}
