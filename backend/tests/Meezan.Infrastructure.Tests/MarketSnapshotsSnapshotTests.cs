using System.Net;
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
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests;

public class MarketSnapshotsSnapshotTests : IClassFixture<MarketSnapshotsSnapshotTests.MarketSnapshotsTestFactory>
{
    private readonly MarketSnapshotsTestFactory _factory;

    public MarketSnapshotsSnapshotTests(MarketSnapshotsTestFactory factory)
    {
        _factory = factory;
    }

    public class MarketSnapshotsTestFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                var snapshotClientDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMubasherMarketSnapshotClient));
                if (snapshotClientDescriptor != null) services.Remove(snapshotClientDescriptor);
                services.AddSingleton<IMubasherMarketSnapshotClient, FakeMubasherMarketSnapshotClient>();
            });
        }
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMarketSnapshotRepository>();

        var existing = await repo.GetLatestAsync();
        if (!existing.Any())
        {
            await repo.UpsertAsync(new MarketIndexSnapshot
            {
                Kind = "Index",
                Code = "EGX30",
                MubasherCode = "EGX30",
                Value = 30000m,
                Change = 100m,
                ChangePct = 0.33m,
                SourceDateText = "17/10/2026",
                Source = "Mubasher",
                FetchedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc)
            });
        }
    }

    [Fact]
    public async Task GetAll_Returns200_MatchesSnapshot()
    {
        await SeedDataAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/market-snapshots");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("MarketSnapshots_GetAll_Success", (int)response.StatusCode, content);
    }
}
