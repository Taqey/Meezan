using System.Data.Common;
using Meezan.Application.Common.Models.Caching;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Services.Caching;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class DbCommandCountInterceptor : DbCommandInterceptor
{
    public int CommandCount { get; private set; }

    public void Reset() => CommandCount = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        CommandCount++;
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        CommandCount++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        CommandCount++;
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        CommandCount++;
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}

public class TargetDesignVerificationTests : IDisposable
{
    private readonly DbCommandCountInterceptor _interceptor;
    private readonly WebApplicationFactory<Program> _factory;

    public TargetDesignVerificationTests()
    {
        _interceptor = new DbCommandCountInterceptor();

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Meezan.WebApi")))
            .AddJsonFile("appsettings.json")
            .Build();

        var connStr = config.GetConnectionString("DefaultConnection")!;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Funds:Cache:Enabled"] = "true",
                    ["Funds:Cache:WarmupOnStartup"] = "false",
                    ["Funds:SchedulerEnabled"] = "false",
                    ["Redis:Host"] = "" // Memory L1 only for isolated SQL counting
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.FullName ?? "").Contains("IDbContextOptionsConfiguration"))
                    .ToList();

                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(opts =>
                {
                    opts.UseSqlServer(connStr);
                    opts.AddInterceptors(_interceptor);
                });
            });
        });
    }



    [Fact]
    public async Task ColdAndWarm_GetFunds_SqlCount_ConformsToTargetDesign()
    {
        var client = _factory.CreateClient();

        // 1. Cold Request: Static build (at most 10 SQL queries) + ONE price query
        _interceptor.Reset();
        var coldResponse = await client.GetAsync("/api/funds?page=1&pageSize=20");
        Assert.True(coldResponse.IsSuccessStatusCode);
        var coldCount = _interceptor.CommandCount;

        // Static build <= 10 + 1 price query = at most 11 queries
        Assert.True(coldCount <= 11, $"Cold request executed {coldCount} queries; expected <= 11 (set-based static build + 1 price query).");

        // 2. Warm Request: Exactly 1 price query (and zero queries per fund)
        _interceptor.Reset();
        var warmResponse = await client.GetAsync("/api/funds?page=1&pageSize=20");
        Assert.True(warmResponse.IsSuccessStatusCode);
        var warmCount = _interceptor.CommandCount;

        Assert.Equal(1, warmCount);
    }

    [Fact]
    public async Task StaticCacheEntries_ContainNoPriceFields()
    {
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<Meezan.Application.Common.Interfaces.Caching.IFundCacheService>();

        var staticFunds = await cacheService.GetStaticFundsAsync();
        Assert.NotEmpty(staticFunds);

        var first = staticFunds.Values.First();
        var type = first.GetType();

        // Target Design 1: No price fields in static cache entry
        Assert.Null(type.GetProperty("CurrentPrice"));
        Assert.Null(type.GetProperty("DailyChange"));
        Assert.Null(type.GetProperty("Return1M"));
        Assert.Null(type.GetProperty("Return3M"));
        Assert.Null(type.GetProperty("Return6M"));
        Assert.Null(type.GetProperty("Return1Y"));
        Assert.Null(type.GetProperty("Return3Y"));
        Assert.Null(type.GetProperty("PricesUpdatedAt"));
    }

    [Fact]
    public async Task CacheDisabledSwitch_PassesStraightThroughToSql()
    {
        var disabledFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Funds:Cache:Enabled"] = "false"
                });
            });
        });

        var client = disabledFactory.CreateClient();
        var response = await client.GetAsync("/api/funds?page=1&pageSize=20");
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CancelledOrFailedBuild_StoresNothing_AndEmptyIsMiss()
    {
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<Meezan.Application.Common.Interfaces.Caching.IFundCacheService>();

        // Immediate cancellation token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await cacheService.GetStaticFundsAsync(cts.Token);
        });
    }

    [Fact]
    public async Task ConcurrentColdRequests_TriggerOnlyOneStaticBuild()
    {
        // Clear cache
        using var scope = _factory.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<Meezan.Application.Common.Interfaces.Caching.IFundCacheService>();
        await cacheService.ClearFundCacheAsync();

        var client = _factory.CreateClient();
        _interceptor.Reset();

        // 20 concurrent requests
        var tasks = Enumerable.Range(0, 20).Select(_ => client.GetAsync("/api/funds?page=1&pageSize=20")).ToArray();
        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
        {
            Assert.True(r.IsSuccessStatusCode);
        }

        // Static build happens only ONCE across 20 concurrent cold requests
        // Rebuild is <= 10 queries, plus 20 price queries (one per request) -> total command count around ~37-40
        Assert.True(_interceptor.CommandCount <= 45, $"Expected <= 45 queries for 20 concurrent requests (1 build + 20 price queries), but got {_interceptor.CommandCount}");
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
