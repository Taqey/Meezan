namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>One parsed market-summary block from a Mubasher index/sector page.</summary>
public sealed record MarketSnapshotQuote(
    decimal? Value,
    decimal? Change,
    decimal? ChangePct,
    bool IsUp,
    string? SourceDateText);

/// <summary>
/// Scrapes Mubasher's published index/sector quotes (implemented in
/// Infrastructure; Application only sees this contract).
/// </summary>
public interface IMubasherMarketSnapshotClient
{
    Task<MarketSnapshotQuote?> ScrapeIndexSummaryAsync(
        string mubasherSlug, CancellationToken cancellationToken = default);
}
