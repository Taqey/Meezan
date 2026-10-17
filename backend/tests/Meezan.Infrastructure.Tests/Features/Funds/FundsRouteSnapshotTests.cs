using System.Net;
using System.Text.Json;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// Snapshot + contract tests for FundsController (public/anonymous endpoints).
/// Uses a unique in-memory database seeded with minimal data per test class.
/// These run GREEN against the original controller; they lock the JSON contract
/// so the CQRS migration cannot silently break any response shape.
/// </summary>
public class FundsRouteSnapshotTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    // Seeded entity IDs
    private int _fund1Id;
    private int _fund2Id;
    private string _fund1Slug = string.Empty;
    private int _catId;
    private int _typeDef1Id;

    public FundsRouteSnapshotTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "funds-snap-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "funds-snap-key",
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

        // --- Type definition ---
        var typeDef = new FundTypeDefinition
        {
            Slug = "equity",
            NameAr = "أسهم",
            NameEn = "Equity",
            IsVisible = true,
            IsDeleted = false,
            DisplayOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FundTypeDefinitions.Add(typeDef);

        // --- Category ---
        var cat = new FundCategory
        {
            NameAr = "فئة الاختبار",
            NameEn = "Test Category",
            Slug = "test-category",
            Icon = "icon",
            Color = "#fff",
            IsAutoDerived = false,
            DisplayOrder = 1,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.FundCategories.Add(cat);

        // --- Funds ---
        var f1 = MakeFund("TST001", "صندوق الاختبار الأول", FundType.Equity, aum: 1000m, isIslamic: true);
        f1.Slug = "test-fund-one";
        var f2 = MakeFund("TST002", "صندوق الاختبار الثاني", FundType.FixedIncome, aum: 500m, isIslamic: false);

        _db.Funds.AddRange(f1, f2);
        await _db.SaveChangesAsync();

        _fund1Id = f1.Id;
        _fund2Id = f2.Id;
        _fund1Slug = f1.Slug;
        _catId = cat.Id;
        _typeDef1Id = typeDef.Id;
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static Fund MakeFund(string code, string nameAr, FundType type,
        decimal? aum = null, bool isIslamic = false)
    {
        return new Fund
        {
            SndukId = Math.Abs(code.GetHashCode()),
            PublicId = $"snap-{code.ToLowerInvariant()}",
            Slug = $"snap-{code.ToLowerInvariant()}",
            NameAr = nameAr,
            NameEn = nameAr,
            Code = code,
            Type = type,
            IsActive = true,
            IsIslamic = isIslamic,
            Aum = aum,
            ManagementFee = 1.5m,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // ── GET /api/funds ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFunds_NoFilter_Returns200_WithPagedShape()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("totalCount", out _));
        Assert.True(doc.RootElement.TryGetProperty("page", out _));
        Assert.True(doc.RootElement.TryGetProperty("pageSize", out _));
        Assert.True(doc.RootElement.TryGetProperty("totalPages", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_NoFilter", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_IsIslamic_True_Returns200_OnlyIslamicFunds()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?isIslamic=true");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items");
        Assert.All(items.EnumerateArray(), item =>
            Assert.True(item.GetProperty("isIslamic").GetBoolean()));
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_IsIslamic", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_Search_Returns200_MatchingFunds()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?search=الأول");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_Search", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_NodeFilter_UnknownSlug_Returns200_EmptyItems()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?node=nonexistent-slug-xyz");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_Node_Unknown", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_TypeFilter_LegacyUnknown_Returns200_Empty()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?type=unknown");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_Type_Unknown", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_SortByAum_Desc_Returns200()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?sortBy=aum&sortDir=desc");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_SortAum_Desc", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFunds_Page2_Returns200_EmptyItems()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds?page=2&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetList_Page2", (int)r.StatusCode, body);
    }

    // ── GET /api/funds/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task GetFundById_ExistingId_Returns200_WithDetailShape()
    {
        var client = CreateClient();
        var r = await client.GetAsync($"/api/funds/{_fund1Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
        Assert.True(doc.RootElement.TryGetProperty("nameAr", out _));
        Assert.True(doc.RootElement.TryGetProperty("priceHistory", out _));
        Assert.True(doc.RootElement.TryGetProperty("analysis", out _));
        Assert.True(doc.RootElement.TryGetProperty("similarFunds", out _));
        Assert.True(doc.RootElement.TryGetProperty("categoryMemberships", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetById_Existing", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFundById_NonExistentId_Returns404()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/999999");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetById_NotFound", (int)r.StatusCode, await r.Content.ReadAsStringAsync());
    }

    // ── GET /api/funds/slug/{slug} ───────────────────────────────────────

    [Fact]
    public async Task GetFundBySlug_ExistingSlug_Returns200_WithDetailShape()
    {
        var client = CreateClient();
        var r = await client.GetAsync($"/api/funds/slug/{_fund1Slug}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetBySlug_Existing", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetFundBySlug_NonExistentSlug_Returns404()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/slug/slug-that-does-not-exist-xyz");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetBySlug_NotFound", (int)r.StatusCode, await r.Content.ReadAsStringAsync());
    }

    // ── GET /api/funds/stats ─────────────────────────────────────────────

    [Fact]
    public async Task GetStats_Returns200_WithExpectedShape()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/stats");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("totalCount", out _));
        Assert.True(doc.RootElement.TryGetProperty("islamicCount", out _));
        Assert.True(doc.RootElement.TryGetProperty("totalAum", out _));
        Assert.True(doc.RootElement.TryGetProperty("bestDailyChange", out _));
        Assert.True(doc.RootElement.TryGetProperty("bestDailyChangeCode", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_Stats", (int)r.StatusCode, body);
    }

    // ── GET /api/funds/categories ────────────────────────────────────────

    [Fact]
    public async Task GetCategories_Returns200_WithCategoryList()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/categories");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);
        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("nameAr", out _));
        Assert.True(first.TryGetProperty("slug", out _));
        Assert.True(first.TryGetProperty("displayOrder", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetCategories", (int)r.StatusCode, body);
    }

    // ── GET /api/funds/types ─────────────────────────────────────────────

    [Fact]
    public async Task GetTypes_Returns200_WithTypeList()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/types");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);
        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("slug", out _));
        Assert.True(first.TryGetProperty("nameAr", out _));
        Assert.True(first.TryGetProperty("displayOrder", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_GetTypes", (int)r.StatusCode, body);
    }

    // ── GET /api/funds/classification-tree ───────────────────────────────

    [Fact]
    public async Task GetClassificationTree_Returns200_EmptyWhenNoNodes()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/classification-tree");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        // With no classification nodes seeded the result must be an empty array
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        SnapshotAssert.MatchesStoredSnapshot("Funds_ClassificationTree_Empty", (int)r.StatusCode, body);
    }

    [Fact]
    public async Task GetClassificationTree_WithNode_Returns200_ContainsNode()
    {
        // Seed a visible node with a fund classified into it
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var node = new FundClassificationNode
        {
            Slug = "snap-equity",
            NameAr = "أسهم",
            NameEn = "Equity",
            Level = 1,
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.FundClassificationNodes.Add(node);
        await db.SaveChangesAsync();

        // Classify fund1 into this node via override
        var override_ = new FundClassificationOverride
        {
            FundId = _fund1Id,
            NodeId = node.Id,
            Action = ClassificationOverrideAction.Include,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        };
        db.FundClassificationOverrides.Add(override_);
        await db.SaveChangesAsync();

        var client = CreateClient();
        var r = await client.GetAsync("/api/funds/classification-tree");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() > 0);
        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("slug", out _));
        Assert.True(first.TryGetProperty("directFundCount", out _));
        Assert.True(first.TryGetProperty("totalFundCount", out _));
        SnapshotAssert.MatchesStoredSnapshot("Funds_ClassificationTree_WithNode", (int)r.StatusCode, body);
    }
}
