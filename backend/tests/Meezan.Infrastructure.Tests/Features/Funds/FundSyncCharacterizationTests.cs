using Meezan.Application.Common.Exceptions;
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
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;
using Meezan.Application.Features.Funds.Sync.Commands.BackfillFundLogos;
using Meezan.Application.Features.Funds.Sync.Commands.RunFundSync;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// Characterization of the fund sync pipeline through <see cref="RunFundSyncCommand"/>
/// with a fake ISndukFundClient (no network) on an isolated in-memory database.
/// These pin the behavior of the pipeline as it exists, independent of how it is organised.
/// </summary>
public class FundSyncCharacterizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly FakeSndukFundClient _client = new();
    private readonly FakeLogoStorage _logos = new();

    public FundSyncCharacterizationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "fund-sync-tests-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "fund-sync-test-key",
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
                    o.UseInMemoryDatabase(dbName);
                    o.ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.RemoveAll<ISndukFundClient>();
                services.AddSingleton<ISndukFundClient>(_client);
                services.RemoveAll<IFundLogoStorage>();
                services.AddSingleton<IFundLogoStorage>(_logos);
            });
        });
    }

    private sealed class FakeLogoStorage : IFundLogoStorage
    {
        public List<string> Stored { get; } = new();
        public bool IsEnabled => true;
        public bool Exists(string relativePath) => false;

        public Task<string?> StoreAsync(string sourceUrl, string fundSlug, CancellationToken ct = default)
        {
            Stored.Add(fundSlug);
            return Task.FromResult<string?>("funds/" + fundSlug + ".png");
        }
    }

    private async Task<T> WithDb<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private async Task ConfigureSettingsAsync(bool enabled = true, bool preventOverlap = true)
    {
        await WithDb(async db =>
        {
            var s = await db.FundSyncSettings.FirstOrDefaultAsync();
            if (s == null)
            {
                s = new FundSyncSettings { Id = 1 };
                db.FundSyncSettings.Add(s);
            }
            s.IsEnabled = enabled;
            s.PreventOverlap = preventOverlap;
            s.RequestDelaySeconds = 0;
            await db.SaveChangesAsync();
            return 0;
        });
    }

    private async Task<FundSyncLog> RunAsync(string by = "Manual")
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunFundSyncCommand(by));
    }

    private void ScriptThreeFunds()
    {
        _client.Funds.Clear();
        _client.Funds.Add(FakeSndukFundClient.Dto(101, "AAA", "10.00", islamic: true));
        _client.Funds.Add(FakeSndukFundClient.Dto(102, "BBB", "20.00"));
        _client.Funds.Add(FakeSndukFundClient.Dto(103, "CCC", "30.00", name: "صندوق يتبع EGX30"));
        foreach (var id in new long[] { 101, 102, 103 })
        {
            _client.Prices[id] = new List<SndukPricePointDto>
            {
                FakeSndukFundClient.Point("2026-10-01", "10.0"),
                FakeSndukFundClient.Point("2026-10-02", "10.5"),
                FakeSndukFundClient.Point("2026-10-03", "11.0")
            };
        }
        _client.Competing[101] = new long[] { 102, 103 };
        _client.Competing[102] = new long[] { 101 };
    }

    [Fact]
    public async Task EmptyDatabase_Run_CreatesFundsPricesAnalysesAndLogs()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();

        var log = await RunAsync();

        Assert.Equal(FundSyncStatus.Success, log.Status);
        Assert.Equal("Manual", log.TriggeredBy);
        Assert.Equal(3, log.TotalFunds);
        Assert.Equal(3, log.UpdatedFunds);
        Assert.Equal(0, log.FailedFunds);
        Assert.NotNull(log.FinishedAt);

        await WithDb(async db =>
        {
            Assert.Equal(3, await db.Funds.CountAsync());
            Assert.Equal(9, await db.FundPriceHistories.CountAsync());
            Assert.Equal(3, await db.FundAnalyses.CountAsync());
            Assert.Equal(3, await db.FundSyncLogDetails.CountAsync(d => d.Status == FundSyncDetailStatus.Success));
            Assert.Equal(1, await db.FundSyncLogs.CountAsync());
            var a = await db.Funds.SingleAsync(f => f.SndukId == 101);
            Assert.Equal(10.00m, a.CurrentPrice);
            Assert.Equal("AAA", a.Code);
            Assert.Equal("funds/fund-aaa.png", a.LocalLogoPath);
            // Auto-derived baseline categories were seeded and the Islamic fund was auto-included.
            Assert.True(await db.FundCategories.AnyAsync(c => c.Slug == "sharia-compliant"));
            Assert.True(await db.FundCategoryOverrides.AnyAsync(o => o.FundId == a.Id && o.CreatedBy == "AutoSync"));
            return 0;
        });
    }

    [Fact]
    public async Task SecondRun_IsIdempotent_NoDuplicateFundsOrPriceRows()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await RunAsync();
        var overridesAfterFirst = await WithDb(db => db.FundCategoryOverrides.CountAsync());

        var second = await RunAsync();

        Assert.Equal(FundSyncStatus.Success, second.Status);
        await WithDb(async db =>
        {
            Assert.Equal(3, await db.Funds.CountAsync());
            Assert.Equal(9, await db.FundPriceHistories.CountAsync());
            Assert.Equal(3, await db.FundAnalyses.CountAsync());
            Assert.Equal(overridesAfterFirst, await db.FundCategoryOverrides.CountAsync());
            Assert.Equal(2, await db.FundSyncLogs.CountAsync(l => l.Status == FundSyncStatus.Success));

            var dupPrices = (await db.FundPriceHistories.ToListAsync())
                .GroupBy(p => (p.FundId, p.Date)).Count(g => g.Count() > 1);
            Assert.Equal(0, dupPrices);
            var dupSimilar = (await db.FundSimilars.ToListAsync())
                .GroupBy(s => (s.FundId, s.SimilarFundId)).Count(g => g.Count() > 1);
            Assert.Equal(0, dupSimilar);
            return 0;
        });
    }

    [Fact]
    public async Task ChangedPrice_UpdatesExistingFund_AndAppendsOnlyNewPriceRow()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await RunAsync();

        _client.Funds[0] = FakeSndukFundClient.Dto(101, "AAA", "12.34", islamic: true);
        _client.Prices[101].Add(FakeSndukFundClient.Point("2026-10-04", "12.34"));
        var log = await RunAsync();

        Assert.Equal(FundSyncStatus.Success, log.Status);
        await WithDb(async db =>
        {
            Assert.Equal(3, await db.Funds.CountAsync());
            var a = await db.Funds.SingleAsync(f => f.SndukId == 101);
            Assert.Equal(12.34m, a.CurrentPrice);
            Assert.Equal(4, await db.FundPriceHistories.CountAsync(p => p.FundId == a.Id));
            Assert.Equal(12.34m, (await db.FundPriceHistories.Where(p => p.FundId == a.Id).OrderByDescending(p => p.Date).FirstAsync()).Price);
            Assert.Equal(20.00m, (await db.Funds.SingleAsync(f => f.SndukId == 102)).CurrentPrice);
            return 0;
        });
    }

    [Fact]
    public async Task OneFundFails_RunIsPartial_FailedFundKeepsOldHistory_OthersUpdated()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await RunAsync();

        // Everything changes upstream, but fund 102's per-fund call fails.
        _client.Funds[0] = FakeSndukFundClient.Dto(101, "AAA", "11.11", islamic: true);
        _client.Funds[1] = FakeSndukFundClient.Dto(102, "BBB", "22.22");
        _client.Funds[2] = FakeSndukFundClient.Dto(103, "CCC", "33.33", name: "صندوق يتبع EGX30");
        foreach (var id in new long[] { 101, 102, 103 })
            _client.Prices[id].Add(FakeSndukFundClient.Point("2026-10-04", "99.0"));
        _client.FailPerformanceFor.Add(102);

        var log = await RunAsync();

        Assert.Equal(FundSyncStatus.Partial, log.Status);
        Assert.Equal(3, log.TotalFunds);
        Assert.Equal(2, log.UpdatedFunds);
        Assert.Equal(1, log.FailedFunds);
        Assert.Equal("1 fund(s) failed to sync", log.ErrorSummary);

        await WithDb(async db =>
        {
            var b = await db.Funds.SingleAsync(f => f.SndukId == 102);
            // Characterized quirk: the fund row itself is upserted + saved BEFORE the per-fund
            // performance call, so a late failure leaves the core row updated; only the
            // price history / analysis / similar data of the failed fund stays at its old state.
            Assert.Equal(22.22m, b.CurrentPrice);
            Assert.Equal(3, await db.FundPriceHistories.CountAsync(p => p.FundId == b.Id));

            var a = await db.Funds.SingleAsync(f => f.SndukId == 101);
            var c = await db.Funds.SingleAsync(f => f.SndukId == 103);
            Assert.Equal(11.11m, a.CurrentPrice);
            Assert.Equal(33.33m, c.CurrentPrice);
            Assert.Equal(4, await db.FundPriceHistories.CountAsync(p => p.FundId == a.Id));
            Assert.Equal(4, await db.FundPriceHistories.CountAsync(p => p.FundId == c.Id));

            var detailB = await db.FundSyncLogDetails.SingleAsync(d => d.FundSyncLogId == log.Id && d.FundCode == "BBB");
            Assert.Equal(FundSyncDetailStatus.Failed, detailB.Status);
            Assert.Contains("simulated transport failure", detailB.ErrorMessage);
            Assert.Equal(2, await db.FundSyncLogDetails.CountAsync(d => d.FundSyncLogId == log.Id && d.Status == FundSyncDetailStatus.Success));
            return 0;
        });
    }

    [Fact]
    public async Task ManualOverrides_AndRenamedCategories_SurviveSync()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await RunAsync();

        int fundA, fundC, shariaId, egx30Id;
        (fundA, fundC, shariaId, egx30Id) = await WithDb(async db =>
        {
            var a = await db.Funds.SingleAsync(f => f.SndukId == 101);
            var c = await db.Funds.SingleAsync(f => f.SndukId == 103);
            var sharia = await db.FundCategories.SingleAsync(x => x.Slug == "sharia-compliant");
            var egx = await db.FundCategories.SingleAsync(x => x.Slug == "index-egx30");

            // Admin excludes the Islamic fund from the auto category, removes the auto include.
            var auto = await db.FundCategoryOverrides.Where(o => o.FundId == a.Id && o.CategoryId == sharia.Id).ToListAsync();
            db.FundCategoryOverrides.RemoveRange(auto);
            db.FundCategoryOverrides.Add(new FundCategoryOverride
            {
                FundId = a.Id, CategoryId = sharia.Id, Action = OverrideAction.Exclude,
                CreatedAt = DateTime.UtcNow, CreatedBy = "admin"
            });
            // Admin includes fund C in a category it would otherwise leave... and renames a category.
            var cOverrides = await db.FundCategoryOverrides.Where(o => o.FundId == c.Id && o.CategoryId == egx.Id).ToListAsync();
            db.FundCategoryOverrides.RemoveRange(cOverrides);
            db.FundCategoryOverrides.Add(new FundCategoryOverride
            {
                FundId = c.Id, CategoryId = egx.Id, Action = OverrideAction.Include,
                CreatedAt = DateTime.UtcNow, CreatedBy = "admin"
            });
            egx.NameAr = "اسم معدل يدوياً";
            await db.SaveChangesAsync();
            return (a.Id, c.Id, sharia.Id, egx.Id);
        });

        await RunAsync();

        await WithDb(async db =>
        {
            var aOverrides = await db.FundCategoryOverrides.Where(o => o.FundId == fundA && o.CategoryId == shariaId).ToListAsync();
            var only = Assert.Single(aOverrides);
            Assert.Equal(OverrideAction.Exclude, only.Action);
            Assert.Equal("admin", only.CreatedBy);

            var cOverrides = await db.FundCategoryOverrides.Where(o => o.FundId == fundC && o.CategoryId == egx30Id).ToListAsync();
            var manual = Assert.Single(cOverrides);
            Assert.Equal("admin", manual.CreatedBy);

            Assert.Equal("اسم معدل يدوياً", (await db.FundCategories.SingleAsync(x => x.Id == egx30Id)).NameAr);
            Assert.Equal(6, await db.FundCategories.CountAsync(x => x.CreatedBy == "AutoSync"));
            return 0;
        });
    }

    [Fact]
    public async Task OverlapLease_SecondRunRejectedWhileOneRuns()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await WithDb(async db =>
        {
            db.FundSyncLogs.Add(new FundSyncLog { Status = FundSyncStatus.Running, TriggeredBy = "Scheduled", RunAt = DateTime.UtcNow.AddMinutes(-5) });
            await db.SaveChangesAsync();
            return 0;
        });

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => RunAsync());

        Assert.Contains("Another sync run is already in progress", ex.Message);
        Assert.Equal(0, _client.ListCalls);
        await WithDb(async db =>
        {
            Assert.Equal(1, await db.FundSyncLogs.CountAsync());
            Assert.Equal(0, await db.Funds.CountAsync());
            return 0;
        });
    }

    [Fact]
    public async Task OverlapLease_StaleRunningRowExpires_AndNewRunProceeds()
    {
        await ConfigureSettingsAsync();
        ScriptThreeFunds();
        await WithDb(async db =>
        {
            db.FundSyncLogs.Add(new FundSyncLog { Status = FundSyncStatus.Running, TriggeredBy = "Scheduled", RunAt = DateTime.UtcNow.AddMinutes(-31) });
            await db.SaveChangesAsync();
            return 0;
        });

        var log = await RunAsync();

        Assert.Equal(FundSyncStatus.Success, log.Status);
        await WithDb(async db =>
        {
            var logs = await db.FundSyncLogs.OrderBy(l => l.RunAt).ToListAsync();
            Assert.Equal(2, logs.Count);
            Assert.Equal(FundSyncStatus.Failed, logs[0].Status);
            Assert.Contains("Stale run lock released", logs[0].ErrorSummary);
            Assert.NotNull(logs[0].FinishedAt);
            Assert.Equal(FundSyncStatus.Success, logs[1].Status);
            return 0;
        });
    }

    [Fact]
    public async Task DisabledSettings_RunIsRejected()
    {
        await ConfigureSettingsAsync(enabled: false);
        ScriptThreeFunds();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => RunAsync());

        Assert.Equal("Fund sync is disabled", ex.Message);
        Assert.Equal(0, _client.ListCalls);
    }

    [Fact]
    public async Task EmptyFundList_MarksRunFailed_AndRejects()
    {
        await ConfigureSettingsAsync();
        _client.Funds.Clear();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => RunAsync());

        Assert.Equal("No funds returned from SNDUK", ex.Message);
        await WithDb(async db =>
        {
            var log = await db.FundSyncLogs.SingleAsync();
            Assert.Equal(FundSyncStatus.Failed, log.Status);
            Assert.Equal("No funds returned from SNDUK", log.ErrorSummary);
            Assert.NotNull(log.FinishedAt);
            return 0;
        });
    }

    [Fact]
    public async Task LogoBackfill_StoresMissingLogos_AndReportsCount()
    {
        await WithDb(async db =>
        {
            db.Funds.Add(NewFund(1, "L1", "https://example.invalid/1.png"));
            db.Funds.Add(NewFund(2, "L2", ""));
            await db.SaveChangesAsync();
            return 0;
        });

        BackfillFundLogosResultDto result;
        using (var scope = _factory.Services.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new BackfillFundLogosCommand());

        Assert.Equal(1, result.Stored);
        await WithDb(async db =>
        {
            Assert.Equal("funds/fund-l1.png", (await db.Funds.SingleAsync(f => f.Code == "L1")).LocalLogoPath);
            Assert.True(string.IsNullOrEmpty((await db.Funds.SingleAsync(f => f.Code == "L2")).LocalLogoPath));
            return 0;
        });
    }

    private static Fund NewFund(long sndukId, string code, string logoUrl) => new()
    {
        SndukId = sndukId,
        PublicId = "P" + sndukId,
        Slug = "fund-" + code.ToLowerInvariant(),
        NameAr = "صندوق " + code,
        NameEn = "Fund " + code,
        Code = code,
        LogoUrl = logoUrl,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
