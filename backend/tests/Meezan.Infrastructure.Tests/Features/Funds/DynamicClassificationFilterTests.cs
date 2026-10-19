using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.DeleteClassificationNode;
using Meezan.Application.Features.Funds.Classification.Commands.AssignClassificationFund;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class DynamicClassificationFilterTests
{
    private ServiceProvider CreateServices(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseInMemoryDatabase(dbName));

        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(configuration);

        services.AddMemoryCache();
        services.AddLogging();

        // Repositories
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundRepository, Persistence.Repositories.Funds.FundRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundClassificationNodeRepository, Persistence.Repositories.Funds.FundClassificationNodeRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundClassificationOverrideRepository, Persistence.Repositories.Funds.FundClassificationOverrideRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundTypeDefinitionRepository, Persistence.Repositories.Funds.FundTypeDefinitionRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundTypeOverrideRepository, Persistence.Repositories.Funds.FundTypeOverrideRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundSyncSettingsRepository, Persistence.Repositories.Funds.FundSyncSettingsRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundCategoryRepository, Persistence.Repositories.Funds.FundCategoryRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundCategoryOverrideRepository, Persistence.Repositories.Funds.FundCategoryOverrideRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundAnalysisRepository, Persistence.Repositories.Funds.FundAnalysisRepository>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundSimilarRepository, Persistence.Repositories.Funds.FundSimilarRepository>();

        // Services
        services.AddScoped<Services.Funds.Queries.FundDetailQuery>();
        services.AddScoped<Application.Common.Interfaces.Queries.Funds.IFundDetailQuery, Services.Funds.Queries.FundDetailQuery>();
        services.AddScoped<Application.Common.Interfaces.Repositories.Funds.IFundClassificationService, Services.Funds.Classification.FundClassificationService>();

        services.Configure<Services.Caching.RedisOptions>(o =>
        {
            o.Host = "";
            o.KeyPrefix = $"test:{Guid.NewGuid():N}:";
        });

        services.AddSingleton<IFundCacheService, Services.Caching.FundCacheService>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task DynamicCascade_CreatesTestNodeAndGrandchild_ReflectedInCachedTree()
    {
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServices(dbName);
        var cacheService = sp.GetRequiredService<IFundCacheService>();

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Root node
            var root = new FundClassificationNode
            {
                Id = 1,
                Slug = "equity",
                NameAr = "أسهم",
                NameEn = "Equity",
                Level = 0,
                IsVisible = true,
                DisplayOrder = 1,
                ChildrenLabelAr = "القطاع"
            };

            // TEST- child node
            var testChild = new FundClassificationNode
            {
                Id = 2,
                ParentId = 1,
                Slug = "TEST-sector",
                NameAr = "قطاع تجريبي",
                NameEn = "Test Sector",
                Level = 1,
                IsVisible = true,
                DisplayOrder = 1,
                ChildrenLabelAr = "التصنيف الفرعي التجريبي"
            };

            // TEST- grandchild node
            var testGrandchild = new FundClassificationNode
            {
                Id = 3,
                ParentId = 2,
                Slug = "TEST-subsector",
                NameAr = "تصنيف فرعي تجريبي",
                NameEn = "Test Subsector",
                Level = 2,
                IsVisible = true,
                DisplayOrder = 1
            };

            db.FundClassificationNodes.AddRange(root, testChild, testGrandchild);

            var fund = new Fund
            {
                Id = 201,
                PublicId = "test-dyn-fund",
                Slug = "test-dyn-fund",
                NameAr = "صندوق تجريبي ديناميكي",
                NameEn = "Dynamic Test Fund",
                Code = "TDF",
                Type = FundType.Equity,
                IsActive = true,
                CurrentPrice = 200m,
                DailyChange = 1.0m,
                Return1Y = 15m
            };
            db.Funds.Add(fund);

            // Assign fund to grandchild node
            db.FundClassificationOverrides.Add(new FundClassificationOverride
            {
                FundId = 201,
                NodeId = 3,
                Action = ClassificationOverrideAction.Include,
                CreatedBy = "DynamicTest"
            });

            await db.SaveChangesAsync();
        }

        // Fetch tree from cache
        var dataset = await cacheService.GetClassificationDatasetAsync();
        Assert.NotNull(dataset);

        // Grandchild node has 1 fund directly and descendants
        var grandchildTree = dataset.Tree.FirstOrDefault(t => t.Slug == "TEST-subsector");
        Assert.NotNull(grandchildTree);
        Assert.Equal(1, grandchildTree.TotalFundCount);

        // Child node has 1 fund via descendants
        var childTree = dataset.Tree.FirstOrDefault(t => t.Slug == "TEST-sector");
        Assert.NotNull(childTree);
        Assert.Equal(1, childTree.TotalFundCount);
        Assert.Equal("التصنيف الفرعي التجريبي", childTree.ChildrenLabelAr);

        // Root node has 1 fund via descendants
        var rootTree = dataset.Tree.FirstOrDefault(t => t.Slug == "equity");
        Assert.NotNull(rootTree);
        Assert.Equal(1, rootTree.TotalFundCount);

        // Test cleanup
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var nodes = await db.FundClassificationNodes.Where(n => n.Slug.StartsWith("TEST-")).ToListAsync();
            db.FundClassificationNodes.RemoveRange(nodes);
            var overrides = await db.FundClassificationOverrides.Where(o => o.FundId == 201).ToListAsync();
            db.FundClassificationOverrides.RemoveRange(overrides);
            var funds = await db.Funds.Where(f => f.Id == 201).ToListAsync();
            db.Funds.RemoveRange(funds);
            await db.SaveChangesAsync();
        }

        await cacheService.InvalidateCatalogAsync();
        var datasetClean = await cacheService.GetClassificationDatasetAsync();
        Assert.DoesNotContain(datasetClean.Tree, t => t.Slug.StartsWith("TEST-"));
    }
}
