using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Meezan.Infrastructure.Tests;

public class FundRegressionTests
{
    private static ApplicationDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Bug1_FundSyncSettings_CanBeUpdatedAndPersisted()
    {
        // Regression test for Bug 1:
        // Updating settings and calling SaveChangesAsync must persist changes to the database.
        var dbName = Guid.NewGuid().ToString();
        await using (var context = CreateInMemoryDbContext(dbName))
        {
            var settings = new FundSyncSettings
            {
                Id = 1,
                IsEnabled = false,
                RunTimeCairo = new TimeOnly(3, 0),
                PreventOverlap = true,
                RequestDelaySeconds = 2,
                MaxRetries = 3,
                RetryBackoffSeconds = 5,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "initial"
            };
            context.FundSyncSettings.Add(settings);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateInMemoryDbContext(dbName))
        {
            var repo = new FundSyncSettingsRepository(context);
            var settings = await repo.GetAsync();
            Assert.NotNull(settings);
            Assert.False(settings.IsEnabled);

            // Mutate settings as done in AdminFundsController.UpdateSyncSettings
            settings.IsEnabled = true;
            settings.RunTimeCairo = new TimeOnly(4, 30);
            settings.UpdatedBy = "admin";
            settings.UpdatedAt = DateTime.UtcNow;

            repo.Update(settings);
            var affected = await context.SaveChangesAsync();
            Assert.True(affected > 0);
        }

        // Verify changes are actually persisted
        await using (var context = CreateInMemoryDbContext(dbName))
        {
            var repo = new FundSyncSettingsRepository(context);
            var updated = await repo.GetAsync();
            Assert.NotNull(updated);
            Assert.True(updated.IsEnabled);
            Assert.Equal(new TimeOnly(4, 30), updated.RunTimeCairo);
            Assert.Equal("admin", updated.UpdatedBy);
        }
    }

    [Fact]
    public void Bug2_SyncSimilarFunds_SignatureReusesFundRepository()
    {
        // Regression test for Bug 2:
        // SyncSimilarFundsAsync must take IFundRepository directly instead of IServiceProvider
        // to avoid unmanaged IServiceScope leaks.
        var method = typeof(Meezan.Infrastructure.Services.FundSyncService)
            .GetMethod("SyncSimilarFundsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
        var parameters = method.GetParameters();

        // Must take IFundRepository, NOT IServiceProvider
        Assert.Contains(parameters, p => p.ParameterType == typeof(IFundRepository));
        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(IServiceProvider));
    }

    [Fact]
    public async Task Bug3_FundCategoryFilter_HonorsAutoSyncAndManualOverrides()
    {
        // Regression test for Bug 3:
        // Fund category filter in FundRepository must honor auto-derived overrides (CreatedBy = "AutoSync")
        // and manual exclude overrides.
        var dbName = Guid.NewGuid().ToString();
        await using (var context = CreateInMemoryDbContext(dbName))
        {
            var cat = new FundCategory
            {
                Id = 10,
                Slug = "sharia-compliant",
                NameAr = "متوافق مع الشريعة",
                NameEn = "Sharia Compliant",
                IsAutoDerived = true,
                IsVisible = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.FundCategories.Add(cat);

            var fund1 = new Fund
            {
                Id = 101,
                SndukId = 101,
                PublicId = "F101",
                Slug = "f101",
                NameAr = "صندوق أ",
                NameEn = "Fund A",
                Code = "FA",
                Type = FundType.Equity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SndukCreatedAt = DateTime.UtcNow,
                SndukUpdatedAt = DateTime.UtcNow
            };

            var fund2 = new Fund
            {
                Id = 102,
                SndukId = 102,
                PublicId = "F102",
                Slug = "f102",
                NameAr = "صندوق ب",
                NameEn = "Fund B",
                Code = "FB",
                Type = FundType.Equity,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SndukCreatedAt = DateTime.UtcNow,
                SndukUpdatedAt = DateTime.UtcNow
            };

            context.Funds.AddRange(fund1, fund2);

            // AutoSync included fund1
            context.FundCategoryOverrides.Add(new FundCategoryOverride
            {
                Id = 1,
                CategoryId = 10,
                FundId = 101,
                Action = OverrideAction.Include,
                CreatedBy = "AutoSync",
                CreatedAt = DateTime.UtcNow
            });

            // Fund 2 has an exclude override
            context.FundCategoryOverrides.Add(new FundCategoryOverride
            {
                Id = 2,
                CategoryId = 10,
                FundId = 102,
                Action = OverrideAction.Exclude,
                CreatedBy = "admin",
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }

        await using (var context = CreateInMemoryDbContext(dbName))
        {
            var repo = new FundRepository(context);
            var (items, total) = await repo.QueryPagedAsync(new FundListFilter
            {
                CategoryId = 10,
                Page = 1,
                PageSize = 10
            });

            Assert.Equal(1, total);
            Assert.Single(items);
            Assert.Equal(101, items[0].Id);
        }
    }
}
