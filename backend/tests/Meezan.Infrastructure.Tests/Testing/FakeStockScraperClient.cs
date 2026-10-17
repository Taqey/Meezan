using Meezan.Application.Common.Interfaces.Integrations;

namespace Meezan.Infrastructure.Tests.Testing;

public class FakeStockScraperClient : IStockScraperClient
{
    public Task<ScrapedMarketData?> ScrapeMarketDataAsync(string ticker, CancellationToken ct = default)
    {
        return Task.FromResult<ScrapedMarketData?>(new ScrapedMarketData(
            Ticker: ticker,
            NominalValue: 10m,
            MarketValue: 1000000m,
            BookValue: 15m,
            PbRatio: 2.5m,
            Eps: 3.5m,
            PeRatio: 10.5m,
            Currency: "EGP",
            High: 40m,
            Low: 35m,
            Open: 36m,
            ClosingPrice: 38m,
            SourceLastUpdateText: "17/10/2026 14:30:00",
            IsNotFound: false
        ));
    }

    public Task<ScrapedSupportResistance?> ScrapeSupportResistanceAsync(string ticker, CancellationToken ct = default)
    {
        return Task.FromResult<ScrapedSupportResistance?>(new ScrapedSupportResistance(
            Ticker: ticker,
            LastPrice: 38m,
            ChangePct: 2.5m,
            Pivot: 37m,
            R1: 39m,
            R2: 41m,
            S1: 35m,
            S2: 33m
        ));
    }
}
