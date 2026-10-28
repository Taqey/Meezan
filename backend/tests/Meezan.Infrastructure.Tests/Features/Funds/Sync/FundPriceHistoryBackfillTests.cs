using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class FundPriceHistoryBackfillTests
{
    private sealed class InMemoryFundRepository : IFundRepository
    {
        public List<Fund> Funds { get; } = [];

        public Task<List<Fund>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Funds.ToList());
        public Task<List<Fund>> GetAllStaticAsync(CancellationToken cancellationToken = default) => Task.FromResult(Funds.ToList());
        public Task<(List<Fund> Items, int TotalCount)> QueryPagedAsync(FundListFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult((Funds.ToList(), Funds.Count));
        public Task<Fund?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Funds.FirstOrDefault(f => f.Id == id));
        public Task<Fund?> GetBySndukIdAsync(long sndukId, CancellationToken cancellationToken = default) => Task.FromResult(Funds.FirstOrDefault(f => f.SndukId == sndukId));
        public Task<Fund?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(Funds.FirstOrDefault(f => f.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        public Task<Fund?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(Funds.FirstOrDefault(f => f.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(Fund fund, CancellationToken cancellationToken = default) { Funds.Add(fund); return Task.CompletedTask; }
        public void Update(Fund fund) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class InMemoryPriceRepository : IFundPriceHistoryRepository
    {
        public List<FundPriceHistory> Items { get; } = [];

        public Task<List<FundPriceHistory>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(p => p.FundId == fundId).OrderBy(p => p.Date).ToList());

        public Task<List<FundPriceHistory>> GetByFundIdAndDateRangeAsync(int fundId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(p => p.FundId == fundId && p.Date >= from && p.Date <= to).OrderBy(p => p.Date).ToList());

        public Task<FundPriceHistory?> GetLatestAsync(int fundId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(p => p.FundId == fundId).OrderByDescending(p => p.Date).FirstOrDefault());

        public Task<FundPriceHistory?> GetByFundIdAndDateAsync(int fundId, DateTime date, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(p => p.FundId == fundId && p.Date.Date == date.Date));

        public Task<DateTime?> GetMaxDateAsync(int fundId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(p => p.FundId == fundId).Select(p => (DateTime?)p.Date).DefaultIfEmpty(null).Max());

        public Task AddRangeAsync(IEnumerable<FundPriceHistory> history, CancellationToken cancellationToken = default)
        {
            Items.AddRange(history);
            return Task.CompletedTask;
        }

        public void UpdateRange(IEnumerable<FundPriceHistory> history) { }
    }

    private sealed class FakeSndukClient : ISndukFundClient
    {
        public SndukPerformanceResponse? PerformanceResponse { get; set; }

        public Task<SndukFundListResponse?> GetFundListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<SndukFundListResponse?>(null);

        public Task<SndukPerformanceResponse?> GetFundPerformanceAsync(long fundId, string timeframe = "1Y", CancellationToken cancellationToken = default) =>
            Task.FromResult(PerformanceResponse);

        public Task<SndukAnalysisResponse?> GetFundAnalysisAsync(long fundId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SndukAnalysisResponse?>(null);

        public Task<SndukCompetingResponse?> GetFundCompetingAsync(long fundId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SndukCompetingResponse?>(null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.FromResult(1);
        }
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeSyncLogRepository : IFundSyncLogRepository
    {
        public List<FundSyncLog> Logs { get; } = [];
        public FundSyncLog? LastAdded { get; private set; }
        public Task<FundSyncLog?> GetLatestAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Logs.OrderByDescending(l => l.RunAt).FirstOrDefault());
        public Task<(List<FundSyncLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult((Logs.ToList(), Logs.Count));
        public Task AddAsync(FundSyncLog log, CancellationToken cancellationToken = default)
        {
            log.Id = Logs.Count + 1;
            Logs.Add(log);
            LastAdded = log;
            return Task.CompletedTask;
        }
        public void Update(FundSyncLog log) { }
    }

    private sealed class FakeSyncLogDetailRepository : IFundSyncLogDetailRepository
    {
        public List<FundSyncLogDetail> Details { get; } = [];
        public Task AddRangeAsync(IEnumerable<FundSyncLogDetail> details, CancellationToken cancellationToken = default)
        {
            Details.AddRange(details);
            return Task.CompletedTask;
        }
        public Task<List<FundSyncLogDetail>> GetByLogIdAsync(int logId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Details.Where(d => d.FundSyncLogId == logId).ToList());
    }

    [Fact]
    public async Task BackfillPriceHistoryAsync_DryRun_DetectsGapsWithoutWriting()
    {
        // Arrange
        var fundRepo = new InMemoryFundRepository();
        fundRepo.Funds.Add(new Fund
        {
            Id = 5,
            Code = "BWA",
            SndukId = 1,
            NameAr = "صندوق وفرة",
            Slug = "beltone-wafra"
        });

        var priceRepo = new InMemoryPriceRepository();
        priceRepo.Items.Add(new FundPriceHistory { FundId = 5, Date = new DateTime(2026, 10, 15), Price = 2.1040m });
        priceRepo.Items.Add(new FundPriceHistory { FundId = 5, Date = new DateTime(2026, 10, 26), Price = 1.8699m });

        var sndukClient = new FakeSndukClient
        {
            PerformanceResponse = new SndukPerformanceResponse
            {
                Data = new[]
                {
                    new SndukPricePointDto { Date = "2026-10-15", Price = "2.1040", Change = "0.81" },
                    new SndukPricePointDto { Date = "2026-10-18", Price = "2.1142", Change = "0.48" },
                    new SndukPricePointDto { Date = "2026-10-19", Price = "2.0329", Change = "-3.85" },
                    new SndukPricePointDto { Date = "2026-10-26", Price = "1.8960", Change = "-5.26" }
                }
            }
        };

        var uow = new FakeUnitOfWork();

        var services = new ServiceCollection();
        services.AddSingleton<IFundRepository>(fundRepo);
        services.AddSingleton<IFundPriceHistoryRepository>(priceRepo);
        services.AddSingleton<ISndukFundClient>(sndukClient);
        services.AddSingleton<IUnitOfWork>(uow);
        var serviceProvider = services.BuildServiceProvider();

        var syncService = new FundSyncService(serviceProvider, NullLogger<FundSyncService>.Instance, new FundSyncProgressTracker());

        // Act - Dry Run
        var dryResult = await syncService.BackfillPriceHistoryAsync(dryRun: true, specificFundId: 5);

        // Assert - Dry Run
        Assert.True(dryResult.IsDryRun);
        Assert.Equal(1, dryResult.TotalFundsInspected);
        Assert.Equal(1, dryResult.FundsWithGaps);
        Assert.Equal(2, dryResult.TotalMissingPointsFound); // 2026-10-18 and 2026-10-19
        Assert.Equal(0, dryResult.TotalPointsInserted);
        Assert.Equal(2, priceRepo.Items.Count); // Unchanged
        Assert.Contains("2026-10-18", dryResult.FundDetails[0].MissingDates);
        Assert.Contains("2026-10-19", dryResult.FundDetails[0].MissingDates);
        Assert.Equal(0, uow.SaveChangesCalls);
    }

    [Fact]
    public async Task BackfillPriceHistoryAsync_RealRun_InsertsMissingPointsIdempotently()
    {
        // Arrange
        var fundRepo = new InMemoryFundRepository();
        fundRepo.Funds.Add(new Fund
        {
            Id = 5,
            Code = "BWA",
            SndukId = 1,
            NameAr = "صندوق وفرة",
            Slug = "beltone-wafra"
        });

        var priceRepo = new InMemoryPriceRepository();
        priceRepo.Items.Add(new FundPriceHistory { FundId = 5, Date = new DateTime(2026, 10, 15), Price = 2.1040m });
        priceRepo.Items.Add(new FundPriceHistory { FundId = 5, Date = new DateTime(2026, 10, 26), Price = 1.8699m });

        var sndukClient = new FakeSndukClient
        {
            PerformanceResponse = new SndukPerformanceResponse
            {
                Data = new[]
                {
                    new SndukPricePointDto { Date = "2026-10-15", Price = "2.1040", Change = "0.81" },
                    new SndukPricePointDto { Date = "2026-10-18", Price = "2.1142", Change = "0.48" },
                    new SndukPricePointDto { Date = "2026-10-19", Price = "2.0329", Change = "-3.85" },
                    new SndukPricePointDto { Date = "2026-10-26", Price = "1.8960", Change = "-5.26" }
                }
            }
        };

        var uow = new FakeUnitOfWork();

        var services = new ServiceCollection();
        services.AddSingleton<IFundRepository>(fundRepo);
        services.AddSingleton<IFundPriceHistoryRepository>(priceRepo);
        services.AddSingleton<ISndukFundClient>(sndukClient);
        services.AddSingleton<IUnitOfWork>(uow);
        var serviceProvider = services.BuildServiceProvider();

        var syncService = new FundSyncService(serviceProvider, NullLogger<FundSyncService>.Instance, new FundSyncProgressTracker());

        // Act 1: Real execution
        var realResult = await syncService.BackfillPriceHistoryAsync(dryRun: false, specificFundId: 5);

        // Assert 1:
        Assert.False(realResult.IsDryRun);
        Assert.Equal(2, realResult.TotalPointsInserted);
        Assert.Equal(4, priceRepo.Items.Count);
        Assert.Contains(priceRepo.Items, p => p.Date == new DateTime(2026, 10, 18) && p.Price == 2.1142m);
        Assert.Contains(priceRepo.Items, p => p.Date == new DateTime(2026, 10, 19) && p.Price == 2.0329m);
        Assert.Equal(1, uow.SaveChangesCalls);

        // Act 2: Re-run (Idempotency test)
        var secondRunResult = await syncService.BackfillPriceHistoryAsync(dryRun: false, specificFundId: 5);

        // Assert 2:
        Assert.Equal(0, secondRunResult.FundsWithGaps);
        Assert.Equal(0, secondRunResult.TotalMissingPointsFound);
        Assert.Equal(0, secondRunResult.TotalPointsInserted);
        Assert.Equal(4, priceRepo.Items.Count); // Zero duplicates
    }

    [Fact]
    public async Task FetchMissingPricesForFundAsync_FillsOnlyGaps_LeavesExistingByteIdentical_AndEnforcesCooldown()
    {
        FundSyncService.ResetSingleFundCooldownsForTesting();

        var fundRepo = new InMemoryFundRepository();
        fundRepo.Funds.Add(new Fund
        {
            Id = 50,
            Code = "BWA",
            SndukId = 1001,
            NameAr = "صندوق وفرة",
            Slug = "beltone-wafra"
        });

        var fixedCreatedAt1 = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc);
        var fixedCreatedAt2 = new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Utc);

        var existingPt1 = new FundPriceHistory
        {
            Id = 901,
            FundId = 50,
            Date = new DateTime(2026, 10, 15),
            Price = 2.1040m,
            Change = 0.81m,
            Performance = 5.12m,
            CreatedAt = fixedCreatedAt1
        };
        var existingPt2 = new FundPriceHistory
        {
            Id = 902,
            FundId = 50,
            Date = new DateTime(2026, 10, 26),
            Price = 1.8699m,
            Change = -5.00m,
            Performance = 2.00m,
            CreatedAt = fixedCreatedAt2
        };

        var priceRepo = new InMemoryPriceRepository();
        priceRepo.Items.Add(existingPt1);
        priceRepo.Items.Add(existingPt2);

        // Source has a different price for 2026-10-26 (1.9999 vs 1.8699) to prove existing points are NEVER overwritten
        // And lacks 2026-10-20 (Tuesday holiday) to verify SourceMissingDates reporting
        var sndukClient = new FakeSndukClient
        {
            PerformanceResponse = new SndukPerformanceResponse
            {
                Data = new[]
                {
                    new SndukPricePointDto { Date = "2026-10-15", Price = "2.1040", Change = "0.81" },
                    new SndukPricePointDto { Date = "2026-10-18", Price = "2.1142", Change = "0.48" },
                    new SndukPricePointDto { Date = "2026-10-19", Price = "2.0329", Change = "-3.85" },
                    // 2026-10-20 missing in source (holiday)
                    new SndukPricePointDto { Date = "2026-10-21", Price = "2.0117", Change = "-1.04" },
                    new SndukPricePointDto { Date = "2026-10-22", Price = "1.9997", Change = "-0.60" },
                    new SndukPricePointDto { Date = "2026-10-25", Price = "1.9702", Change = "-1.48" },
                    new SndukPricePointDto { Date = "2026-10-26", Price = "1.9999", Change = "9.99" }
                }
            }
        };

        var uow = new FakeUnitOfWork();
        var logRepo = new FakeSyncLogRepository();
        var detailRepo = new FakeSyncLogDetailRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IFundRepository>(fundRepo);
        services.AddSingleton<IFundPriceHistoryRepository>(priceRepo);
        services.AddSingleton<ISndukFundClient>(sndukClient);
        services.AddSingleton<IFundSyncLogRepository>(logRepo);
        services.AddSingleton<IFundSyncLogDetailRepository>(detailRepo);
        services.AddSingleton<IUnitOfWork>(uow);
        var sp = services.BuildServiceProvider();

        var syncService = new FundSyncService(sp, NullLogger<FundSyncService>.Instance, new FundSyncProgressTracker());

        // 1. First click fills only missing dates (5 dates: 10/18, 10/19, 10/21, 10/22, 10/25)
        var res1 = await syncService.FetchMissingPricesForFundAsync(50, "AdminTester");
        Assert.False(res1.NothingMissing);
        Assert.False(res1.OnCooldown);
        Assert.Equal(5, res1.AddedCount);
        Assert.Equal(7, priceRepo.Items.Count);
        Assert.Contains("2026-10-20", res1.SourceMissingDates);

        // Existing rows remain byte-identical (same object reference, Id, Price, Change, Performance, CreatedAt)
        var storedPt1 = priceRepo.Items.Single(p => p.Date == new DateTime(2026, 10, 15));
        var storedPt2 = priceRepo.Items.Single(p => p.Date == new DateTime(2026, 10, 26));
        Assert.Same(existingPt1, storedPt1);
        Assert.Same(existingPt2, storedPt2);
        Assert.Equal(901, storedPt1.Id);
        Assert.Equal(2.1040m, storedPt1.Price);
        Assert.Equal(fixedCreatedAt1, storedPt1.CreatedAt);
        Assert.Equal(902, storedPt2.Id);
        Assert.Equal(1.8699m, storedPt2.Price); // NOT overwritten by 1.9999
        Assert.Equal(-5.00m, storedPt2.Change);
        Assert.Equal(fixedCreatedAt2, storedPt2.CreatedAt);

        // Logged run
        Assert.Single(logRepo.Logs);
        Assert.Equal("SingleFundMissingPrices", logRepo.Logs[0].Mode);
        Assert.Equal("AdminTester", logRepo.Logs[0].TriggeredBy);

        // 2. Immediate second click hits cooldown
        var resCooldown = await syncService.FetchMissingPricesForFundAsync(50, "AdminTester", bypassCooldown: false);
        Assert.True(resCooldown.OnCooldown);
        Assert.True(resCooldown.CooldownRemainingSeconds > 0);

        // 3. Second click after cooldown adds 0 and says nothing is missing
        var res2 = await syncService.FetchMissingPricesForFundAsync(50, "AdminTester", bypassCooldown: true);
        Assert.True(res2.NothingMissing);
        Assert.Equal(0, res2.AddedCount);
        Assert.Equal("لا توجد أسعار ناقصة", res2.Message);
        Assert.Equal(7, priceRepo.Items.Count);
    }

    [Fact]
    public async Task FetchMissingPricesForFundAsync_NoSourceData_ReportsClearly()
    {
        FundSyncService.ResetSingleFundCooldownsForTesting();

        var fundRepo = new InMemoryFundRepository();
        fundRepo.Funds.Add(new Fund
        {
            Id = 51,
            Code = "EMPTY",
            SndukId = 1002,
            NameAr = "صندوق بدون تاريخ",
            Slug = "empty-fund"
        });

        var priceRepo = new InMemoryPriceRepository();
        var sndukClient = new FakeSndukClient { PerformanceResponse = new SndukPerformanceResponse { Data = [] } };
        var uow = new FakeUnitOfWork();
        var logRepo = new FakeSyncLogRepository();
        var detailRepo = new FakeSyncLogDetailRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IFundRepository>(fundRepo);
        services.AddSingleton<IFundPriceHistoryRepository>(priceRepo);
        services.AddSingleton<ISndukFundClient>(sndukClient);
        services.AddSingleton<IFundSyncLogRepository>(logRepo);
        services.AddSingleton<IFundSyncLogDetailRepository>(detailRepo);
        services.AddSingleton<IUnitOfWork>(uow);
        var sp = services.BuildServiceProvider();

        var syncService = new FundSyncService(sp, NullLogger<FundSyncService>.Instance, new FundSyncProgressTracker());

        var res = await syncService.FetchMissingPricesForFundAsync(51, "AdminTester");
        Assert.True(res.NothingMissing);
        Assert.Equal(0, res.AddedCount);
        Assert.Contains("لا تتوفر بيانات تاريخية", res.Message);
    }
}
