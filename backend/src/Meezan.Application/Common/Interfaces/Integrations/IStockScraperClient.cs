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

/// <summary>
/// Raw data returned by the Mubasher support/resistance page scraper for a single stock.
/// </summary>
public record ScrapedSupportResistance(
    string Ticker,
    decimal? LastPrice,
    decimal? ChangePct,
    decimal? Pivot,
    decimal? R1,
    decimal? R2,
    decimal? S1,
    decimal? S2
);

public interface IStockScraperClient
{
    /// <summary>Scrapes market-data for one stock ticker (e.g. "AMOC").</summary>
    Task<ScrapedMarketData?> ScrapeMarketDataAsync(string ticker, CancellationToken ct = default);

    /// <summary>Scrapes support/resistance levels for one stock ticker.</summary>
    Task<ScrapedSupportResistance?> ScrapeSupportResistanceAsync(string ticker, CancellationToken ct = default);
}
