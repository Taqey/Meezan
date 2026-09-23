using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces;

/// <summary>
/// Holds the fully-scraped and computed data for a single stock,
/// collected in memory before the atomic commit.
/// </summary>
public sealed record StockScrapedSnapshot(
    int StockId,
    string Ticker,
    StockMarketData? MarketData,
    StockFairValue? FairValue,
    List<StockFairValueMethod>? FairValueMethods,
    StockSupportResistance? SupportResistance
);

/// <summary>
/// Atomically replaces the contents of the live market-data tables
/// (StockMarketData, StockFairValues, StockFairValueMethods, StockSupportResistance)
/// with the supplied batch — all in a single database transaction.
///
/// If the transaction fails or the caller passes an empty batch,
/// the live tables are left completely unchanged.
/// </summary>
public interface IScrapeBatchCommitter
{
    Task CommitLiveAsync(IReadOnlyList<StockScrapedSnapshot> batch, CancellationToken ct = default);
    Task CommitSlowAsync(IReadOnlyList<StockScrapedSnapshot> batch, CancellationToken ct = default);
}
