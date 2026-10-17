using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;

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
