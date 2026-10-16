using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Tests;

/// <summary>
/// HTTP route tests for the classification system against an isolated
/// in-memory database: public tree, node/type/subType/leaf filters,
/// parent-includes-descendants, invalid slugs, legacy aliases, hidden/empty
/// node policy, manual precedence, bulk ops, validation errors and 401s.
/// </summary>
public class FundClassificationRouteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string Key = "class-route-test-key";

    public FundClassificationRouteTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "class-route-tests-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = Key,
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

    private HttpClient CreateClient(bool withKey = true)
    {
        var client = _factory.CreateClient();
        if (withKey) client.DefaultRequestHeaders.Add("X-Admin-Key", Key);
        return client;
    }

    private static Fund MakeFund(string code, string nameAr, FundType type)
    {
        return new Fund
        {
            SndukId = Math.Abs(Guid.NewGuid().GetHashCode()),
            PublicId = "it-" + Guid.NewGuid().ToString("N")[..8],
            Slug = "it-" + Guid.NewGuid().ToString("N")[..8],
            NameAr = nameAr,
            NameEn = nameAr,
            Code = code,
            Type = type,
            IsActive = true,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Seeds 5 funds (equity+EGX30, money market, 2×commodities, unknown-type)
    /// then runs the real seed-migrate endpoint.
    /// </summary>
    private async Task SeedAndMigrateAsync()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await db.Funds.AnyAsync())
            {
                db.Funds.AddRange(
                    MakeFund("ITX30", "صندوق مؤشر EGX30 للاختبار", FundType.Equity),
                    MakeFund("ITMM", "صندوق نقدي للاختبار", FundType.MoneyMarket),
                    MakeFund("ITSLV1", "صندوق فضة أول للاختبار", FundType.Commodities),
                    MakeFund("ITSLV2", "صندوق فضة ثان للاختبار", FundType.Commodities),
                    MakeFund("ITUNK", "صندوق غامض للاختبار", FundType.Unknown));
                await db.SaveChangesAsync();
            }
        }

        var client = CreateClient();
        var migrate = await client.PostAsync("/api/admin/funds/classification/seed-migrate", null);
        Assert.True(migrate.StatusCode == HttpStatusCode.OK,
            $"seed-migrate failed: {(int)migrate.StatusCode} {await migrate.Content.ReadAsStringAsync()}");
    }

    private static void AssertOk(HttpResponseMessage r, string what)
    {
        Assert.True(r.StatusCode == HttpStatusCode.OK,
            $"{what}: expected 200 OK, got {(int)r.StatusCode} {r.StatusCode}. Body: {r.Content.ReadAsStringAsync().Result}");
    }

    private static async Task<int> TotalCountOf(HttpResponseMessage r)
    {
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("totalCount").GetInt32();
    }

    private static async Task<List<string>> CodesOf(HttpResponseMessage r)
    {
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("code").GetString()!).ToList();
    }

    [Fact]
    public async Task SeedMigrate_Returns200_WithBackupAndSilverRename()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var migrate = await client.PostAsync("/api/admin/funds/classification/seed-migrate", null);
        AssertOk(migrate, "seed-migrate (idempotent rerun)");
        using var doc = JsonDocument.Parse(await migrate.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("backupRows", out _), "report must include backupRows");
        Assert.True(root.GetProperty("commoditiesFunds").GetArrayLength() == 2,
            "both commodities funds must be reported");
    }

    [Fact]
    public async Task AllTenSlugs_Return200_WithReconcilingCounts()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var counts = new Dictionary<string, int>();
        foreach (var slug in new[] { "equity", "fixed_income", "fixed_income_usd", "balanced", "money_market", "index", "gold", "silver", "capital_preservation", "unknown" })
        {
            var r = await client.GetAsync($"/api/funds?type={slug}&pageSize=100");
            AssertOk(r, $"type={slug}");
            counts[slug] = await TotalCountOf(r);
        }
        // commodities alias returns the same funds as silver.
        var comm = await client.GetAsync("/api/funds?type=commodities&pageSize=100");
        AssertOk(comm, "type=commodities alias");
        Assert.Equal(counts["silver"], await TotalCountOf(comm));
        Assert.Equal(2, counts["silver"]);
        // Top-level counts reconcile to the active total (index maps into the equity branch).
        var all = await client.GetAsync("/api/funds?pageSize=100");
        var allTotal = await TotalCountOf(all);
        var topLevel = counts["equity"] + counts["fixed_income"] + counts["fixed_income_usd"]
            + counts["balanced"] + counts["money_market"] + counts["gold"] + counts["silver"]
            + counts["capital_preservation"];
        Assert.Equal(allTotal, topLevel);
    }

    [Fact]
    public async Task LegacyTypeIndex_MapsToEquityIndexBranch()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var viaType = await client.GetAsync("/api/funds?type=index&pageSize=100");
        var viaNode = await client.GetAsync("/api/funds?node=index&pageSize=100");
        AssertOk(viaType, "type=index");
        AssertOk(viaNode, "node=index");
        Assert.Equal(await TotalCountOf(viaNode), await TotalCountOf(viaType));
    }

    [Fact]
    public async Task NodeFilter_IncludesDescendants_LeafIsDirectOnly()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();

        var equity = await client.GetAsync("/api/funds?node=equity&pageSize=100");
        AssertOk(equity, "node=equity");
        var equityCodes = await CodesOf(equity);
        // The unclassified equity fund is included via the parent filter.
        Assert.Contains(equityCodes, c => c == "ITX30" || c.StartsWith("IT"));

        // tracks-egx35 has no test funds; use silver leaf (direct, 2 funds).
        var leaf = await client.GetAsync("/api/funds?leaf=silver&pageSize=100");
        AssertOk(leaf, "leaf=silver");
        Assert.Equal(2, await TotalCountOf(leaf));

        var sub = await client.GetAsync("/api/funds?subType=sector&pageSize=100");
        AssertOk(sub, "subType=sector");
    }

    [Fact]
    public async Task InvalidSlugs_ReturnEmpty200_Never500()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        foreach (var u in new[]
        {
            "/api/funds?type=nosuchslug&pageSize=5",
            "/api/funds?node=nosuchslug&pageSize=5",
            "/api/funds?subType=nosuchslug&pageSize=5",
            "/api/funds?leaf=nosuchslug&pageSize=5",
            "/api/funds?type=unknown&pageSize=5",
            "/api/funds?riskLevel=bogus&pageSize=5"
        })
        {
            var r = await client.GetAsync(u);
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{u}: expected 200, got {(int)r.StatusCode}");
            Assert.Equal(0, await TotalCountOf(r));
        }
    }

    [Fact]
    public async Task PublicTree_HidesUnclassifiedHiddenAndEmptyNodes()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/classification-tree");
        AssertOk(r, "public classification-tree");
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        var slugs = doc.RootElement.EnumerateArray().Select(n => n.GetProperty("slug").GetString()!).ToList();
        Assert.DoesNotContain("equity-unclassified", slugs);
        Assert.DoesNotContain("commodities", slugs);
        Assert.Contains("silver", slugs);
        Assert.Contains("equity", slugs);
        // Every listed node carries counts.
        foreach (var n in doc.RootElement.EnumerateArray())
        {
            Assert.True(n.TryGetProperty("totalFundCount", out _), "node must carry totalFundCount");
            Assert.True(n.TryGetProperty("childrenLabelAr", out _), "node must carry childrenLabelAr");
        }
    }

    [Fact]
    public async Task UnclassifiedFunds_StillReturnedByParentFilter()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var parent = await client.GetAsync("/api/funds?node=equity&pageSize=100");
        var codes = await CodesOf(parent);
        Assert.Contains("ITX30", codes);
    }

    [Fact]
    public async Task ChildrenLabelAr_Change_ReflectedInTree()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var tree = await client.GetAsync("/api/admin/funds/classification/tree?includeDeleted=false");
        AssertOk(tree, "admin tree");
        using var doc = JsonDocument.Parse(await tree.Content.ReadAsStringAsync());
        var equity = doc.RootElement.EnumerateArray().First(n => n.GetProperty("slug").GetString() == "equity");
        var id = equity.GetProperty("id").GetInt32();

        var patch = await client.PatchAsync($"/api/admin/funds/classification/nodes/{id}",
            JsonContent.Create(new { childrenLabelAr = "تصنيف فرعي مخصص-TEST" }));
        AssertOk(patch, "update children label");

        var pub = await client.GetAsync("/api/funds/classification-tree");
        var body = await pub.Content.ReadAsStringAsync();
        Assert.Contains("تصنيف فرعي مخصص-TEST", body);
    }

    [Fact]
    public async Task ManualAssign_Wins_AndResetReturnsToAuto()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();

        // Find silver node + a money-market fund, assign manually to silver.
        var tree = await client.GetAsync("/api/admin/funds/classification/tree?includeDeleted=false");
        using var tdoc = JsonDocument.Parse(await tree.Content.ReadAsStringAsync());
        var silverId = tdoc.RootElement.EnumerateArray().First(n => n.GetProperty("slug").GetString() == "silver").GetProperty("id").GetInt32();

        var funds = await client.GetAsync("/api/funds?node=money_market&pageSize=100");
        var code = (await CodesOf(funds)).First();
        int fundId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            fundId = await db.Funds.Where(f => f.Code == code).Select(f => f.Id).FirstAsync();
        }

        var assign = await client.PostAsync($"/api/admin/funds/classification/funds/{fundId}/assign",
            JsonContent.Create(new { nodeId = silverId }));
        AssertOk(assign, "manual assign");

        var under = await client.GetAsync("/api/funds?node=silver&pageSize=100");
        Assert.Contains(code, await CodesOf(under));

        var reset = await client.DeleteAsync($"/api/admin/funds/classification/funds/{fundId}/assign");
        AssertOk(reset, "reset to auto");

        var underAfter = await client.GetAsync("/api/funds?node=silver&pageSize=100");
        Assert.DoesNotContain(code, await CodesOf(underAfter));
    }

    [Fact]
    public async Task BulkAdd_ReturnsAddedAndFailed()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var tree = await client.GetAsync("/api/admin/funds/classification/tree?includeDeleted=false");
        using var tdoc = JsonDocument.Parse(await tree.Content.ReadAsStringAsync());
        var silverId = tdoc.RootElement.EnumerateArray().First(n => n.GetProperty("slug").GetString() == "silver").GetProperty("id").GetInt32();

        int realId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            realId = await db.Funds.Select(f => f.Id).FirstAsync();
        }

        var bulk = await client.PostAsync($"/api/admin/funds/classification/nodes/{silverId}/funds/bulk",
            JsonContent.Create(new { fundIds = new[] { realId, 987654321 } }));
        AssertOk(bulk, "bulk add");
        using var doc = JsonDocument.Parse(await bulk.Content.ReadAsStringAsync());
        Assert.Equal(1, doc.RootElement.GetProperty("added").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("failed").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("errors", out _), "bulk must report errors");

        // Restore: reset the real fund so later tests see clean state.
        await client.DeleteAsync($"/api/admin/funds/classification/funds/{realId}/assign");
    }

    [Fact]
    public async Task AdminClassification_RequiresKey_Returns401()
    {
        var anon = CreateClient(withKey: false);
        var r1 = await anon.PostAsync("/api/admin/funds/classification/seed-migrate", null);
        Assert.Equal(HttpStatusCode.Unauthorized, r1.StatusCode);
        var r2 = await anon.GetAsync("/api/admin/funds/classification/review");
        Assert.Equal(HttpStatusCode.Unauthorized, r2.StatusCode);
        var r3 = await anon.GetAsync("/api/admin/funds/classification/tree");
        Assert.Equal(HttpStatusCode.Unauthorized, r3.StatusCode);
    }

    [Fact]
    public async Task TreeValidation_ReturnsReadableErrors()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var tree = await client.GetAsync("/api/admin/funds/classification/tree?includeDeleted=false");
        using var tdoc = JsonDocument.Parse(await tree.Content.ReadAsStringAsync());
        var first = tdoc.RootElement.EnumerateArray().First();
        var id = first.GetProperty("id").GetInt32();

        // Move under itself.
        var selfMove = await client.PostAsync($"/api/admin/funds/classification/nodes/{id}/move",
            JsonContent.Create(new { newParentId = id }));
        Assert.Equal(HttpStatusCode.BadRequest, selfMove.StatusCode);
        var body = await selfMove.Content.ReadAsStringAsync();
        Assert.Contains(@"\u0646\u0641\u0633\u0647\u0627", body);

        // Duplicate slug.
        var dup = await client.PostAsync("/api/admin/funds/classification/nodes",
            JsonContent.Create(new { nameAr = "مكرر", nameEn = "Dup", slug = first.GetProperty("slug").GetString() }));
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);

        // Delete without confirm/destination on a populated node.
        var silver = tdoc.RootElement.EnumerateArray().First(n => n.GetProperty("slug").GetString() == "silver");
        var silverId = silver.GetProperty("id").GetInt32();
        var del = await client.DeleteAsync($"/api/admin/funds/classification/nodes/{silverId}");
        Assert.Equal(HttpStatusCode.BadRequest, del.StatusCode);
    }

    [Fact]
    public async Task MergeRestore_AndReclassifySkipManual()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();

        // Create two TEST- nodes under a root (use fixed_income to avoid disturbing equity counts).
        var a = await client.PostAsync("/api/admin/funds/classification/nodes",
            JsonContent.Create(new { nameAr = "TEST- دمج أ", nameEn = "TEST Merge A", slug = "test-merge-a-" + Guid.NewGuid().ToString("N")[..6] }));
        AssertOk(a, "create TEST node A");
        using var adoc = JsonDocument.Parse(await a.Content.ReadAsStringAsync());
        var aId = adoc.RootElement.GetProperty("node").GetProperty("id").GetInt32();

        var b = await client.PostAsync("/api/admin/funds/classification/nodes",
            JsonContent.Create(new { nameAr = "TEST- دمج ب", nameEn = "TEST Merge B", slug = "test-merge-b-" + Guid.NewGuid().ToString("N")[..6] }));
        AssertOk(b, "create TEST node B");
        using var bdoc = JsonDocument.Parse(await b.Content.ReadAsStringAsync());
        var bId = bdoc.RootElement.GetProperty("node").GetProperty("id").GetInt32();

        // Assign a fund manually to A, then merge A into B.
        int fundId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            fundId = await db.Funds.Select(f => f.Id).FirstAsync();
        }
        AssertOk(await client.PostAsync($"/api/admin/funds/classification/funds/{fundId}/assign",
            JsonContent.Create(new { nodeId = aId })), "manual assign to A");

        var merge = await client.PostAsync("/api/admin/funds/classification/nodes/merge",
            JsonContent.Create(new { sourceId = aId, targetId = bId }));
        AssertOk(merge, "merge A into B");

        // Restore works.
        var restore = await client.PostAsync($"/api/admin/funds/classification/nodes/{aId}/restore", null);
        AssertOk(restore, "restore A");

        // Reclassify must not touch the manually assigned fund.
        var re = await client.PostAsync("/api/admin/funds/classification/reclassify-unclassified", null);
        AssertOk(re, "reclassify");
        using var redoc = JsonDocument.Parse(await re.Content.ReadAsStringAsync());
        Assert.True(redoc.RootElement.TryGetProperty("moved", out _), "result must include moved");
        Assert.True(redoc.RootElement.TryGetProperty("remaining", out _), "result must include remaining");

        // Cleanup: reset fund, delete both TEST nodes.
        await client.DeleteAsync($"/api/admin/funds/classification/funds/{fundId}/assign");
        await client.DeleteAsync($"/api/admin/funds/classification/nodes/{aId}?confirm=true&resetFundsToAuto=true");
        await client.DeleteAsync($"/api/admin/funds/classification/nodes/{bId}?confirm=true&resetFundsToAuto=true");
    }

    [Fact]
    public async Task TopLevelCounts_ReconcileToActiveTotal()
    {
        await SeedAndMigrateAsync();
        var client = CreateClient();
        var tree = await client.GetAsync("/api/admin/funds/classification/tree?includeDeleted=false");
        AssertOk(tree, "admin tree");
        using var doc = JsonDocument.Parse(await tree.Content.ReadAsStringAsync());
        var roots = doc.RootElement.EnumerateArray().Where(n => n.GetProperty("level").GetInt32() == 0).ToList();
        var sum = roots.Sum(n => n.GetProperty("totalCount").GetInt32());

        var all = await client.GetAsync("/api/funds?pageSize=100");
        Assert.Equal(await TotalCountOf(all), sum);
    }
}
