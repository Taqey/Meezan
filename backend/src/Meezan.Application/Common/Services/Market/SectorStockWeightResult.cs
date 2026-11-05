namespace Meezan.Application.Common.Services.Market;

/// <summary>
/// Result of calculating a single stock's sector weight.
/// </summary>
public record SectorStockWeightResult(
    int StockId,
    string Ticker,
    decimal? EffectiveMarketCapEgp,
    decimal? RawWeight,
    decimal? FinalWeight,
    bool IsCapped,
    decimal CappingFactor
);
