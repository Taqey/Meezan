namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>
/// Raw data returned by the Mubasher market-data page scraper for a single stock.
/// All fields are nullable — the scraper sets only what it could parse.
/// </summary>
public record ScrapedMarketData(
    string Ticker,
    decimal? NominalValue,
    decimal? MarketValue,
    decimal? BookValue,
    decimal? PbRatio,
    decimal? Eps,
    decimal? PeRatio,
    decimal? SharesOutstanding = null,
    string? Currency = null,
    decimal? High = null,
    decimal? Low = null,
    decimal? Open = null,
    decimal? ClosingPrice = null,
    string? SourceLastUpdateText = null,
    bool IsNotFound = false
);
