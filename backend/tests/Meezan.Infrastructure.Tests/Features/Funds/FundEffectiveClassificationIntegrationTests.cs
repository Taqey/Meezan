using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Meezan.Infrastructure.Tests.Features.Funds;

public class FundEffectiveClassificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _euroNodeId;
    private int _fundId;

    public FundEffectiveClassificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "funds-euro-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "test-key",
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
            });
        });
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var euroNode = new FundClassificationNode
        {
            Slug = "euro-fixed-income-fund",
            NameAr = "صندوق دخل ثابت باليورو",
            NameEn = "Euro Fixed Income Fund",
            Level = 0,
            ParentId = null,
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        _db.FundClassificationNodes.Add(euroNode);

        var fund = new Fund
        {
            SndukId = 172,
            PublicId = "fund-maksab-euro",
            Slug = "maksab-oz-euro-fund",
            Code = "OZE",
            NameAr = "صندوق مكسب OZ باليورو",
            NameEn = "Maksab OZ Euro Fund",
            Type = FundType.FixedIncome,
            TypeNameAr = "صندوق دخل ثابت",
            TypeNameEn = "Fixed Income Fund",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Funds.Add(fund);
        await _db.SaveChangesAsync();

        _euroNodeId = euroNode.Id;
        _fundId = fund.Id;

        _db.FundClassificationOverrides.Add(new FundClassificationOverride
        {
            FundId = fund.Id,
            NodeId = euroNode.Id,
            Action = ClassificationOverrideAction.Include,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CustomRootNode_ReflectsInListAndDetail()
    {
        var client = _factory.CreateClient();

        // 1. List
        var listRes = await client.GetAsync("/api/funds?search=مكسب");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var listBody = await listRes.Content.ReadAsStringAsync();
        using var listDoc = JsonDocument.Parse(listBody);
        var item = listDoc.RootElement.GetProperty("items")[0];

        Assert.Equal("صندوق دخل ثابت باليورو", item.GetProperty("typeNameAr").GetString());
        var eff = item.GetProperty("effectiveClassification");
        Assert.Equal("euro-fixed-income-fund", eff.GetProperty("slug").GetString());
        Assert.Equal("صندوق دخل ثابت باليورو", eff.GetProperty("rootNameAr").GetString());

        // 2. Detail
        var detailRes = await client.GetAsync($"/api/funds/{_fundId}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
        var detailBody = await detailRes.Content.ReadAsStringAsync();
        using var detailDoc = JsonDocument.Parse(detailBody);
        Assert.Equal("صندوق دخل ثابت باليورو", detailDoc.RootElement.GetProperty("typeNameAr").GetString());
        Assert.Equal("euro-fixed-income-fund", detailDoc.RootElement.GetProperty("effectiveClassification").GetProperty("slug").GetString());

        // 3. Reassign updates immediately
        var altNode = new FundClassificationNode
        {
            Slug = "euro-money-market",
            NameAr = "صندوق سوق نقدي باليورو",
            NameEn = "Euro Money Market Fund",
            Level = 0,
            ParentId = null,
            DisplayOrder = 2,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        _db!.FundClassificationNodes.Add(altNode);
        await _db.SaveChangesAsync();

        _db.FundClassificationOverrides.Add(new FundClassificationOverride
        {
            FundId = _fundId,
            NodeId = altNode.Id,
            Action = ClassificationOverrideAction.Include,
            CreatedBy = "admin",
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        });
        await _db.SaveChangesAsync();

        var reassignRes = await client.GetAsync($"/api/funds/{_fundId}");
        var reassignBody = await reassignRes.Content.ReadAsStringAsync();
        using var reassignDoc = JsonDocument.Parse(reassignBody);
        Assert.Equal("صندوق سوق نقدي باليورو", reassignDoc.RootElement.GetProperty("typeNameAr").GetString());
        Assert.Equal("euro-money-market", reassignDoc.RootElement.GetProperty("effectiveClassification").GetProperty("slug").GetString());
    }
}
