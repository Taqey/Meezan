using Meezan.Application.Common.Interfaces;

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
