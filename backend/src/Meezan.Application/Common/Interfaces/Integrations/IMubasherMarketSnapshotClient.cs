namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>
/// Scrapes Mubasher's published index/sector quotes (implemented in
/// Infrastructure; Application only sees this contract).
/// </summary>
public interface IMubasherMarketSnapshotClient
{
    Task<MarketSnapshotQuote?> ScrapeIndexSummaryAsync(
        string mubasherSlug, CancellationToken cancellationToken = default);
}
