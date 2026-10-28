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
}
