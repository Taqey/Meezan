using System.Net;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Stocks;

public class StocksSnapshotTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public StocksSnapshotTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Stocks.Any(s => s.Ticker == "ETEL"))
        {
            var sector = new Sector
            {
                NameAr = "اتصالات",
                NameEn = "Telecom"
            };
            db.Sectors.Add(sector);

            var stock = new Stock
            {
                Ticker = "ETEL",
                NameAr = "المصرية للاتصالات",
                NameEn = "Telecom Egypt",
                Sector = sector,
                IsActive = true
            };
            db.Stocks.Add(stock);

            var marketData = new StockMarketData
            {
                Stock = stock,
                ClosingPrice = 35.5m,
                MarketValue = 60000000000m,
                PeRatio = 8.5m,
                PbRatio = 1.2m,
                Eps = 4.1m,
                BookValue = 29.5m,
                Currency = "EGP",
                FetchedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
            };
            db.StockMarketData.Add(marketData);

            var fairValue = new StockFairValue
            {
                Stock = stock,
                FairValue = 42.0m,
                PriceComparison = Meezan.Domain.Enums.PriceComparison.Cheap,
                FairValueDiff = 6.5m,
                FairValueDiffPct = 18.3m,
                MethodsUsedCount = 3,
                Confidence = Meezan.Domain.Enums.ValuationConfidence.High,
                ComputedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
            };
            db.StockFairValues.Add(fairValue);

            var sr = new StockSupportResistance
            {
                Stock = stock,
                LastPrice = 35.5m,
                ChangePct = 1.5m,
                Pivot = 35.0m,
                R1 = 36.0m,
                R2 = 37.0m,
                S1 = 34.0m,
                S2 = 33.0m,
                FetchedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
            };
            db.StockSupportResistance.Add(sr);

            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAll_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks?page=1&pageSize=10");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Stocks_GetAll_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetMarketData_Existing_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks/ETEL/market-data");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Stocks_GetMarketData_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetMarketData_NotFound_Returns404_MatchesSnapshot()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks/NONEXISTENT/market-data");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Stocks_GetMarketData_NotFound", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetSupportResistance_Existing_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks/ETEL/support-resistance");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Stocks_GetSupportResistance_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetSupportResistance_NotFound_Returns404_MatchesSnapshot()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks/NONEXISTENT/support-resistance");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Stocks_GetSupportResistance_NotFound", (int)response.StatusCode, content);
    }
}
