namespace Meezan.Application.Common.Interfaces.Integrations;

public interface IStockScraperClient
{
    /// <summary>Scrapes market-data for one stock ticker (e.g. "AMOC").</summary>
    Task<ScrapedMarketData?> ScrapeMarketDataAsync(string ticker, CancellationToken ct = default);

    /// <summary>Scrapes support/resistance levels for one stock ticker.</summary>
    Task<ScrapedSupportResistance?> ScrapeSupportResistanceAsync(string ticker, CancellationToken ct = default);
}
