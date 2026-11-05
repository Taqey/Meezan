namespace Meezan.Application.Common.Services.Market;

/// <summary>
/// Input item for sector weight calculation.
/// </summary>
public record SectorStockWeightInput(
    int StockId,
    string Ticker,
    decimal? MarketValue,
    string? Currency
);
