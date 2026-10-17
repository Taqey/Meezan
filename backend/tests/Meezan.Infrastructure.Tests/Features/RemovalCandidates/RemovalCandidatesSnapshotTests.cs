using System.Net;
using System.Net.Http.Json;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.RemovalCandidates;

public class RemovalCandidatesSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    public RemovalCandidatesSnapshotTests()
    {
        _dbName = "removal-candidates-snap-" + Guid.NewGuid().ToString("N");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Funds:SchedulerEnabled"] = "false"
            }));
            b.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.FullName ?? "").Contains("IDbContextOptionsConfiguration"))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(o =>
                {
                    o.UseInMemoryDatabase(_dbName);
                    o.ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sector = new Sector
        {
            NameAr = "بنوك",
            NameEn = "Banks"
        };
        _db.Sectors.Add(sector);
        await _db.SaveChangesAsync();

        // 1. Deactivated stock
        var s1 = new Stock
        {
            Ticker = "CIB",
            NameAr = "البنك التجاري الدولي",
            NameEn = "Commercial International Bank",
            SectorId = sector.Id,
            IsActive = false,
            DataStatus = StockDataStatus.Active,
            DeactivatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc),
            DeactivationReason = "Operator deactivated",
            MarketData = new StockMarketData
            {
                ClosingPrice = 85.5m,
                FetchedAt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc),
                SourceLastUpdateText = "12:30"
            }
        };

        // 2. NotFoundOnSource via DataStatus
        var s2 = new Stock
        {
            Ticker = "HRHO",
            NameAr = "المجموعة المالية هيرميس",
            NameEn = "EFG Hermes",
            SectorId = sector.Id,
            IsActive = true,
            DataStatus = StockDataStatus.IncompleteNoData,
            MarketData = new StockMarketData
            {
                ClosingPrice = 20.0m,
                FetchedAt = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc),
                SourceLastUpdateText = "14:00"
            }
        };

        // 3. NotFoundOnSource via null MarketData
        var s3 = new Stock
        {
            Ticker = "ORAS",
            NameAr = "أوراسكوم للإنشاء",
            NameEn = "Orascom Construction",
            SectorId = sector.Id,
            IsActive = true,
            DataStatus = StockDataStatus.Active,
            MarketData = null
        };

        // 4. StaleData via Arabic month text
        var s4 = new Stock
        {
            Ticker = "EAST",
            NameAr = "الشرقية للدخان",
            NameEn = "Eastern Company",
            SectorId = sector.Id,
            IsActive = true,
            DataStatus = StockDataStatus.Active,
            MarketData = new StockMarketData
            {
                ClosingPrice = 30.0m,
                FetchedAt = new DateTime(2025, 10, 1, 8, 0, 0, DateTimeKind.Utc),
                SourceLastUpdateText = "15 أكتوبر 2025"
            }
        };

        // 5. Clean active stock (should NOT appear in removal candidates)
        var s5 = new Stock
        {
            Ticker = "ETEL",
            NameAr = "المصرية للاتصالات",
            NameEn = "Telecom Egypt",
            SectorId = sector.Id,
            IsActive = true,
            DataStatus = StockDataStatus.Active,
            MarketData = new StockMarketData
            {
                ClosingPrice = 40.0m,
                FetchedAt = DateTime.UtcNow,
                SourceLastUpdateText = "11:45"
            }
        };

        _db.Stocks.AddRange(s1, s2, s3, s4, s5);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetRemovalCandidates_Returns200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/scraping/removal-candidates");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_GetList", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ConfirmRemovals_EmptyTickers_Returns400()
    {
        var client = _factory.CreateClient();
        var payload = new { tickers = new List<string>(), reason = "Test reason" };
        var res = await client.PostAsJsonAsync("/api/scraping/removal-candidates/confirm", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_Confirm_EmptyTickers", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ConfirmRemovals_Success_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            tickers = new List<string> { "ETEL", "UNKNOWN_TICKER" },
            reason = "Test deactivation"
        };
        var res = await client.PostAsJsonAsync("/api/scraping/removal-candidates/confirm", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_Confirm_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ReactivateSelected_EmptyTickers_Returns400()
    {
        var client = _factory.CreateClient();
        var payload = new { tickers = new List<string>() };
        var res = await client.PostAsJsonAsync("/api/scraping/removal-candidates/reactivate", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_Reactivate_EmptyTickers", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ReactivateSelected_Success_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            tickers = new List<string> { "CIB", "UNKNOWN_TICKER" }
        };
        var res = await client.PostAsJsonAsync("/api/scraping/removal-candidates/reactivate", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_Reactivate_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RefreshSelected_EmptyTickers_Returns400()
    {
        var client = _factory.CreateClient();
        var payload = new { tickers = new List<string>() };
        var res = await client.PostAsJsonAsync("/api/scraping/removal-candidates/refresh", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("RemovalCandidates_Refresh_EmptyTickers", (int)res.StatusCode, content);
    }
}
