using System.Text.Json;
using Meezan.Application.Common.Models.Caching;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Services.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class FundCacheIntegrationTests
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

        // Redis Options (Empty / local fallback)
        services.Configure<RedisOptions>(o =>
        {
            o.Host = "";
            o.KeyPrefix = $"test:{Guid.NewGuid():N}:";
        });

        services.AddSingleton<Application.Common.Interfaces.Caching.IFundCacheService, FundCacheService>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetClassificationAndStaticFunds_WarmCache_ServesExpectedData()
    {
        var dbName = Guid.NewGuid().ToString();
        var sp = CreateServices(dbName);

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var node = new FundClassificationNode
            {
                Id = 10,
                Slug = "test-root",
                NameAr = "جذر تجريبي",
                NameEn = "Test Root",
                Level = 0,
                IsVisible = true,
                DisplayOrder = 1
            };
            db.FundClassificationNodes.Add(node);

            var fund = new Fund
            {
                Id = 101,
                PublicId = "test-fund-1",
                Slug = "test-fund-1",
                NameAr = "صندوق تجريبي",
                NameEn = "Test Fund",
                Code = "TF1",
                Type = FundType.FixedIncome,
                IsActive = true,
                CurrentPrice = 150m,
                DailyChange = 0.5m,
                Return1Y = 12m
            };
            db.Funds.Add(fund);
            db.FundClassificationOverrides.Add(new FundClassificationOverride
            {
                FundId = 101,
                NodeId = 10,
                Action = ClassificationOverrideAction.Include,
                CreatedBy = "Test"
            });
            await db.SaveChangesAsync();
        }

        var cacheService = sp.GetRequiredService<Application.Common.Interfaces.Caching.IFundCacheService>();

        // 1. Initial load (cold)
        var dataset1 = await cacheService.GetClassificationDatasetAsync();
        Assert.NotNull(dataset1);
        Assert.Contains(dataset1.Tree, n => n.Id == 10 || n.Slug == "test-root");

        var staticFunds1 = await cacheService.GetStaticFundsAsync();
        Assert.NotNull(staticFunds1);
        Assert.True(staticFunds1.ContainsKey(101));
        Assert.Equal("صندوق تجريبي", staticFunds1[101].NameAr);

        // 2. Second read (warm cache)
        var dataset2 = await cacheService.GetClassificationDatasetAsync();
        var staticFunds2 = await cacheService.GetStaticFundsAsync();
        Assert.Same(dataset1, dataset2);
        Assert.Same(staticFunds1, staticFunds2);

        // 3. Invalidate catalog
        await cacheService.InvalidateCatalogAsync();
        var dataset3 = await cacheService.GetClassificationDatasetAsync();
        Assert.NotSame(dataset1, dataset3);
    }

    [Fact]
    public async Task RealRedis_IntegrationTest_WithTestPrefix()
    {
        // Real Redis test using connection string from appsettings
        var redisConn = "redis-18909.c98.us-east-1-4.ec2.cloud.redislabs.com:18909,password=gPY5z3aYhAZwojSkIN2QwgcikgvP6pcj,ssl=false,abortConnect=false";
        IConnectionMultiplexer? mux = null;
        try
        {
            mux = await ConnectionMultiplexer.ConnectAsync(redisConn);
        }
        catch
        {
            // If Redis instance is unreachable in test environment, skip gracefully
            return;
        }

        if (mux == null || !mux.IsConnected) return;

        var testPrefix = $"meezan:test:{Guid.NewGuid():N}:";
        try
        {
            var db = mux.GetDatabase();
            var testKey = $"{testPrefix}testkey";
            await db.StringSetAsync(testKey, "hello-redis", TimeSpan.FromSeconds(30));

            var val = await db.StringGetAsync(testKey);
            Assert.Equal("hello-redis", val.ToString());

            // Atomic INCR test
            var verKey = $"{testPrefix}version:catalog";
            var ver1 = await db.StringIncrementAsync(verKey);
            Assert.True(ver1 >= 1);
            var ver2 = await db.StringIncrementAsync(verKey);
            Assert.Equal(ver1 + 1, ver2);
        }
        finally
        {
            // Clean up test keys safely using server SCAN
            var server = mux.GetServer(mux.GetEndPoints()[0]);
            var keys = server.Keys(pattern: $"{testPrefix}*").ToArray();
            var db = mux.GetDatabase();
            foreach (var k in keys)
            {
                await db.KeyDeleteAsync(k);
            }
            await mux.DisposeAsync();
        }
    }
}
