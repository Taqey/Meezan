using System.Net;
using System.Net.Http.Json;
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
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests;

public class ScrapingSnapshotTests : IClassFixture<ScrapingSnapshotTests.ScrapingTestFactory>
{
    private readonly ScrapingTestFactory _factory;

    public ScrapingSnapshotTests(ScrapingTestFactory factory)
    {
        _factory = factory;
    }

    public class ScrapingTestFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                var scraperDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IStockScraperClient));
                if (scraperDescriptor != null) services.Remove(scraperDescriptor);
                services.AddSingleton<IStockScraperClient, FakeStockScraperClient>();
            });
        }
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (!db.Stocks.Any(s => s.Ticker == "TEST"))
        {
            db.Stocks.Add(new Stock
            {
                Ticker = "TEST",
                NameAr = "سهم تجريبي",
                NameEn = "Test Stock",
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!db.ScrapeRunLogs.Any())
        {
            db.ScrapeRunLogs.Add(new ScrapeRunLog
            {
                TriggeredBy = "Manual-Test",
                StartedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
                FinishedAt = new DateTime(2026, 10, 1, 10, 5, 0, DateTimeKind.Utc),
                TotalStocks = 10,
                SucceededStocks = 9,
                FailedStocks = 1,
                ErrorSummary = null
            });
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetStatus_WithoutJobKey_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/scraping/status");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Scraping_GetStatus_Success", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task GetStatus_WithSpecificJobKey_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/scraping/status?jobKey=LiveDaily");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Scraping_GetStatus_WithJobKey", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task RunScrape_LiveDaily_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync("/api/scraping/run?bucket=LiveDaily", null);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Scraping_Run_LiveDaily", (int)response.StatusCode, content);
    }

    [Fact]
    public async Task RunScrape_Unauthorized_MatchesSnapshot()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/scraping/run?bucket=LiveDaily", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Scraping_Run_Unauthorized", (int)response.StatusCode, string.Empty);
    }
}
