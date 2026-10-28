using MediatR;
using Meezan.Application.Common.Exceptions;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Features.Funds.Sync.Commands.RunFundPriceSync;
using Meezan.Application.Features.Funds.Sync.Dtos;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Cryptography;
using System.Text;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class FundPriceSyncTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly FakeSndukFundClient _client = new();

    public FundPriceSyncTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "fund-price-sync-tests-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "price-sync-test-key",
                ["Funds:SchedulerEnabled"] = "false",
                ["Funds:Cache:WarmupOnStartup"] = "false",
                ["Redis:Host"] = ""
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
                    o.UseInMemoryDatabase(dbName);
                    o.ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.RemoveAll<ISndukFundClient>();
                services.AddSingleton<ISndukFundClient>(_client);
            });
        });
    }

    private async Task<T> WithDb<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private async Task SeedBaselineFundsAsync()
    {
        await WithDb(async db =>
        {
            var settings = await db.FundSyncSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                db.FundSyncSettings.Add(new FundSyncSettings
                {
                    Id = 1,
                    IsEnabled = true,
                    PreventOverlap = true,
                    RequestDelaySeconds = 0
                });
            }

            // Seed two existing funds
            db.Funds.AddRange(
                new Fund
                {
                    Id = 1,
                    SndukId = 101,
                    PublicId = "P101",
                    Code = "AAA",
                    NameAr = "صندوق أ",
                    Slug = "fund-aaa",
                    CurrentPrice = 10.0m,
                    DailyChange = 0.1m,
                    DescriptionAr = "Static Description A",
                    InvestmentStrategy = "Static Strategy A",
                    IsActive = true
                },
                new Fund
                {
                    Id = 2,
                    SndukId = 102,
                    PublicId = "P102",
                    Code = "BBB",
                    NameAr = "صندوق ب",
                    Slug = "fund-bbb",
                    CurrentPrice = 20.0m,
                    DailyChange = 0.2m,
                    DescriptionAr = "Static Description B",
                    InvestmentStrategy = "Static Strategy B",
                    IsActive = true
                }
            );

            await db.SaveChangesAsync();
            return 0;
        });
    }

    private async Task<FundPriceSyncResultDto> RunPriceSyncAsync(string by = "Manual")
    {
        using var scope = _factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunFundPriceSyncCommand(by));
        if (result.IsFailure)
        {
            throw new BadRequestException(result.Error.Message);
        }
        return result.Value;
    }

    [Fact]
    public async Task PriceSync_UpdatesPricesOnly_ExactlyOneHttpRequest_StaticTablesUntouched()
    {
        await SeedBaselineFundsAsync();

        // Configure client with updated price for AAA and BBB, plus a new fund CCC (103)
        _client.Funds.Clear();
        _client.Funds.Add(FakeSndukFundClient.Dto(101, "AAA", "15.75"));
        _client.Funds.Add(FakeSndukFundClient.Dto(102, "BBB", "25.50"));
        _client.Funds.Add(FakeSndukFundClient.Dto(103, "CCC", "50.00")); // New fund not in our DB

        var beforeHash = await WithDb(async db =>
        {
            var f = await db.Funds.OrderBy(x => x.Id).ToListAsync();
            var combined = string.Join("|", f.Select(x => $"{x.Id}:{x.NameAr}:{x.DescriptionAr}:{x.InvestmentStrategy}"));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(combined)));
        });

        int listCallsBefore = _client.ListCalls;
        int perfCallsBefore = _client.PerformanceCalls;

        var result = await RunPriceSyncAsync();

        // 1. Exactly ONE HTTP request to SNDUK (list endpoint), zero to performance/analysis/competing
        Assert.Equal(listCallsBefore + 1, _client.ListCalls);
        Assert.Equal(perfCallsBefore, _client.PerformanceCalls);

        // 2. Prices updated
        Assert.Equal(2, result.UpdatedFunds);
        Assert.Equal(1, result.NewInSndukCount); // CCC detected as new
        Assert.Equal(0, result.MissingFromSndukCount);

        await WithDb(async db =>
        {
            var f1 = await db.Funds.FindAsync(1);
            var f2 = await db.Funds.FindAsync(2);
            var f3 = await db.Funds.FirstOrDefaultAsync(x => x.SndukId == 103);

            Assert.NotNull(f1);
            Assert.Equal(15.75m, f1.CurrentPrice);
            Assert.NotNull(f1.PricesUpdatedAt);

            Assert.NotNull(f2);
            Assert.Equal(25.50m, f2.CurrentPrice);
            Assert.NotNull(f2.PricesUpdatedAt);

            // New fund was NOT created
            Assert.Null(f3);

            // Verify price history point upserted
            var history1 = await db.FundPriceHistories.Where(h => h.FundId == 1).ToListAsync();
            Assert.Single(history1);
            Assert.Equal(15.75m, history1[0].Price);

            // Verify static fields unchanged (hash match)
            var fAll = await db.Funds.OrderBy(x => x.Id).ToListAsync();
            var afterCombined = string.Join("|", fAll.Select(x => $"{x.Id}:{x.NameAr}:{x.DescriptionAr}:{x.InvestmentStrategy}"));
            var afterHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(afterCombined)));
            Assert.Equal(beforeHash, afterHash);

            return 0;
        });

        // 3. Second run idempotent
        var secondRun = await RunPriceSyncAsync();
        Assert.Equal(2, secondRun.UpdatedFunds);

        await WithDb(async db =>
        {
            var history1 = await db.FundPriceHistories.Where(h => h.FundId == 1).ToListAsync();
            Assert.Single(history1); // Still single point, not duplicated
            return 0;
        });
    }

    [Fact]
    public async Task PriceSync_MissingAndNewFundsReported_FailureLeavesPricesIntact()
    {
        await SeedBaselineFundsAsync();

        // SNDUK returns only BBB (102) and a new DDD (104). AAA (101) is missing from SNDUK.
        _client.Funds.Clear();
        _client.Funds.Add(FakeSndukFundClient.Dto(102, "BBB", "33.33"));
        _client.Funds.Add(FakeSndukFundClient.Dto(104, "DDD", "77.77"));

        var result = await RunPriceSyncAsync();

        Assert.Equal(1, result.UpdatedFunds);
        Assert.Equal(1, result.NewInSndukCount); // DDD
        Assert.Equal(1, result.MissingFromSndukCount); // AAA

        await WithDb(async db =>
        {
            var f1 = await db.Funds.FindAsync(1);
            var f2 = await db.Funds.FindAsync(2);

            Assert.NotNull(f1);
            Assert.Equal(10.0m, f1.CurrentPrice); // Untouched because missing from payload

            Assert.NotNull(f2);
            Assert.Equal(33.33m, f2.CurrentPrice);
            return 0;
        });
    }

    [Fact]
    public async Task StatusQuery_WhenIdle_ReturnsLatestDbSummary()
    {
        await WithDb(async db =>
        {
            db.FundSyncLogs.Add(new FundSyncLog
            {
                Id = 10,
                Mode = "Prices",
                Status = FundSyncStatus.Success,
                TriggeredBy = "Manual",
                TotalFunds = 150,
                UpdatedFunds = 148,
                FailedFunds = 2,
                RunAt = DateTime.UtcNow.AddMinutes(-10),
                FinishedAt = DateTime.UtcNow.AddMinutes(-9)
            });
            await db.SaveChangesAsync();
            return 0;
        });

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new Meezan.Application.Features.Funds.Sync.Queries.GetFundSyncStatus.GetFundSyncStatusQuery());

        Assert.True(result.IsSuccess);
        var dto = result.Value;
        Assert.False(dto.IsRunning);
        Assert.Equal("Prices", dto.Mode);
        Assert.Equal("Manual", dto.TriggeredBy);
        Assert.Equal(150, dto.TotalFunds);
        Assert.Equal(148, dto.UpdatedCount);
    }

    [Fact]
    public async Task Tracker_TracksInFlightProgress_AndCompletes()
    {
        using var scope = _factory.Services.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<Meezan.Application.Common.Interfaces.Repositories.Funds.IFundSyncProgressTracker>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        tracker.StartRun("Full", "AdminTest", 172);
        tracker.UpdateProgress(50, 48, 2, "BCO", "Syncing prices");

        var statusResult = await sender.Send(new Meezan.Application.Features.Funds.Sync.Queries.GetFundSyncStatus.GetFundSyncStatusQuery());
        Assert.True(statusResult.IsSuccess);
        var status = statusResult.Value;
        Assert.True(status.IsRunning);
        Assert.Equal("Full", status.Mode);
        Assert.Equal(172, status.TotalFunds);
        Assert.Equal(50, status.ProcessedCount);
        Assert.Equal(48, status.UpdatedCount);
        Assert.Equal(2, status.FailedCount);
        Assert.Equal("BCO", status.CurrentFundCode);

        tracker.SetCompleted("Done successfully");
        var completedResult = await sender.Send(new Meezan.Application.Features.Funds.Sync.Queries.GetFundSyncStatus.GetFundSyncStatusQuery());
        Assert.True(completedResult.IsSuccess);
        Assert.False(completedResult.Value.IsRunning);
        Assert.Equal("Done successfully", completedResult.Value.LastResultSummary);
    }

    [Fact]
    public async Task DoubleStart_RejectedByLease_WhenRunIsActive()
    {
        await WithDb(async db =>
        {
            var settings = await db.FundSyncSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                db.FundSyncSettings.Add(new FundSyncSettings
                {
                    Id = 1,
                    IsEnabled = true,
                    PreventOverlap = true,
                    RequestDelaySeconds = 0
                });
            }
            db.FundSyncLogs.Add(new FundSyncLog
            {
                Status = FundSyncStatus.Running,
                TriggeredBy = "Manual",
                Mode = "Prices",
                RunAt = DateTime.UtcNow.AddMinutes(-5)
            });
            await db.SaveChangesAsync();
            return 0;
        });

        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new RunFundPriceSyncCommand("Manual"));

        Assert.True(result.IsFailure);
        Assert.Contains("Another sync run is already in progress", result.Error.Message);
    }
}
