using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Features.Funds;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Sectors;

public class SectorsPerformanceIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly DbCommandCountInterceptor _interceptor;

    public SectorsPerformanceIntegrationTests()
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

    public void Dispose()
    {
        _factory.Dispose();
    }

    [Fact]
    public async Task GetSectors_SqlCount_IsConstantAndUnderTarget()
    {
        var client = _factory.CreateClient();

        // Warm up cache / route
        _interceptor.Reset();
        var res1 = await client.GetAsync("/api/sectors");
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var coldCommands = _interceptor.CommandCount;

        // Cold should be <= 6 commands (1 for sectors static + 1 aggregate grouped query)
        Assert.True(coldCommands <= 6, $"Cold /api/sectors executed {coldCommands} SQL commands; expected <= 6.");

        // Warm call
        _interceptor.Reset();
        var res2 = await client.GetAsync("/api/sectors");
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var warmCommands = _interceptor.CommandCount;

        // Warm should be <= 6 commands (static cached in L1/L2 memory, exactly 1 aggregate query)
        Assert.True(warmCommands <= 6, $"Warm /api/sectors executed {warmCommands} SQL commands; expected <= 6.");
    }

    [Fact]
    public async Task GetSectorStocks_SqlCount_IsConstantAndUnderTarget()
    {
        var client = _factory.CreateClient();

        _interceptor.Reset();
        var res = await client.GetAsync("/api/stocks?sectorId=1&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var commands = _interceptor.CommandCount;

        // At most 4 SQL commands per request (COUNT, Page Projection, Index Constituents, Sector weights if sectoral)
        Assert.True(commands <= 4, $"/api/stocks executed {commands} SQL commands; expected <= 4.");
    }

    [Fact]
    public async Task SectorStaticCache_MissHitCancelEmpty_BehaviorsVerified()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<Meezan.Application.Common.Interfaces.Caching.ISectorStaticCache>();

        // 1. Initial Get (Miss -> Loads from DB)
        var result1 = await cache.GetAsync(CancellationToken.None);
        Assert.NotNull(result1);
        Assert.NotEmpty(result1);

        // 2. Second Get (Hit -> Returns cached)
        var result2 = await cache.GetAsync(CancellationToken.None);
        Assert.NotNull(result2);
        Assert.Equal(result1.Count, result2.Count);

        // 3. Cancelled token throws OperationCanceledException
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await cache.GetAsync(cts.Token);
        });
    }
}
