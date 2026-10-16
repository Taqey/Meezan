using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Meezan.Infrastructure.Tests;

/// <summary>
/// Route-level tests for the admin fund category membership operations that the
/// admin UI calls from the per-fund "التصنيفات" button: add (include), remove
/// (exclude), single reset-to-auto, bulk reset-to-auto, and category deletion.
/// These assert the real HTTP status (200/400/401) — no 404 allowed.
/// </summary>
public class AdminFundsMembershipRouteTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string Key = "integration-test-admin-key";

    public AdminFundsMembershipRouteTests(WebApplicationFactory<Program> factory)
    {
        // IMPORTANT: the app's default connection string points at the shared
        // production database. These tests must never write there, so the DbContext
        // is replaced with a per-run in-memory store.
        var dbName = "funds-route-tests-" + Guid.NewGuid().ToString("N");
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

    private static async Task<(int CategoryId, int FundId)> SeedAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var slug = "it-cat-" + Guid.NewGuid().ToString("N")[..8];
        var cat = new Domain.Entities.FundCategory
        {
            Slug = slug,
            NameAr = "تصنيف اختبار",
            NameEn = "IT Category",
            Icon = "", Color = "",
            IsAutoDerived = false, IsVisible = true,
            DisplayOrder = 999,
            IsDeleted = false, DeletedReason = "",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test", UpdatedBy = "test"
        };
        db.FundCategories.Add(cat);

        var fund = db.Funds.FirstOrDefault();
        if (fund == null)
        {
            fund = new Domain.Entities.Fund
            {
                SndukId = DateTime.UtcNow.Ticks,
                PublicId = "it-" + Guid.NewGuid().ToString("N")[..8],
                Slug = "it-fund-" + Guid.NewGuid().ToString("N")[..8],
                NameAr = "صندوق اختبار", NameEn = "IT Fund",
                Code = "IT001",
                Type = Domain.Entities.FundType.Equity,
                IsActive = true,
                SndukCreatedAt = DateTime.UtcNow, SndukUpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            db.Funds.Add(fund);
        }

        await db.SaveChangesAsync();
        return (cat.Id, fund.Id);
    }

    private static void AssertOk(HttpResponseMessage r, string what)
    {
        Assert.True(r.StatusCode == HttpStatusCode.OK,
            $"{what}: expected 200 OK, got {(int)r.StatusCode} {r.StatusCode}. Body: {r.Content.ReadAsStringAsync().Result}");
    }

    [Fact]
    public async Task Include_Then_Exclude_Then_Reset_All_Return200_Never404()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var client = CreateClient();

        var add = await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null);
        AssertOk(add, "add fund to category (include)");

        var remove = await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/exclude", null);
        AssertOk(remove, "remove fund from category (exclude)");

        var resetOne = await client.DeleteAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/override");
        AssertOk(resetOne, "reset single override to auto");
    }

    [Fact]
    public async Task BulkResetFundOverrides_Returns200_WithClearedCount()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var client = CreateClient();

        AssertOk(await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null), "seed include");

        var bulk = await client.DeleteAsync($"/api/admin/funds/funds/{fundId}/overrides");
        AssertOk(bulk, "bulk reset fund overrides");

        var body = await bulk.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("clearedCount", out var cc), "response must contain clearedCount");
        Assert.True(cc.GetInt32() >= 1, "clearedCount should count the manual override just created");
    }

    [Fact]
    public async Task BulkResetCategoryOverrides_Returns200()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var client = CreateClient();

        AssertOk(await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null), "seed include");

        var bulk = await client.DeleteAsync($"/api/admin/funds/categories/{catId}/overrides");
        AssertOk(bulk, "bulk reset category overrides");
    }

    [Fact]
    public async Task DeleteCategory_WithFunds_Returns400_UntilConfirmed_Then200()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var client = CreateClient();

        AssertOk(await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null), "seed include");

        // Unconfirmed delete on a non-empty category must be rejected with 400 + fundCount.
        var unconfirmed = await client.DeleteAsync($"/api/admin/funds/categories/{catId}");
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmed.StatusCode);
        var payload = await unconfirmed.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);
        Assert.True(doc.RootElement.TryGetProperty("fundCount", out var fc), "400 body must include fundCount");
        Assert.True(fc.GetInt32() >= 1);

        // Confirmed delete succeeds.
        var confirmed = await client.DeleteAsync($"/api/admin/funds/categories/{catId}?confirm=true");
        AssertOk(confirmed, "confirmed category delete");
    }

    [Fact]
    public async Task MembershipRoutes_RequireAdminKey_Return401Without()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var anon = CreateClient(withKey: false);

        var r1 = await anon.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null);
        Assert.Equal(HttpStatusCode.Unauthorized, r1.StatusCode);

        var r2 = await anon.DeleteAsync($"/api/admin/funds/funds/{fundId}/overrides");
        Assert.Equal(HttpStatusCode.Unauthorized, r2.StatusCode);

        var r3 = await anon.DeleteAsync($"/api/admin/funds/categories/{catId}?confirm=true");
        Assert.Equal(HttpStatusCode.Unauthorized, r3.StatusCode);
    }

    [Fact]
    public async Task CategoryFunds_ListsMembers_WithSourceAndReason()
    {
        var (catId, fundId) = await SeedAsync(_factory);
        var client = CreateClient();

        AssertOk(await client.PostAsync($"/api/admin/funds/funds/{fundId}/categories/{catId}/include", null), "seed include");

        var list = await client.GetAsync($"/api/admin/funds/categories/{catId}/funds");
        AssertOk(list, "list category funds");

        var body = await list.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1, "the included fund must be listed");
        var first = items[0];
        Assert.True(first.TryGetProperty("source", out _), "member must carry a source badge");
        Assert.True(first.TryGetProperty("reason", out _), "member must carry a reason");
    }
}