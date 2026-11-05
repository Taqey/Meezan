using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Interfaces.Integrations;

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
