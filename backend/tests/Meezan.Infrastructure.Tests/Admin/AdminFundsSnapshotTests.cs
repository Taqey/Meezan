using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Meezan.Infrastructure.Tests.Admin;

public class AdminFundsSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private const string AdminKey = "admin-snap-test-key";
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _catId1;
    private int _catId2;
    private int _fundId;

    public AdminFundsSnapshotTests()
    {
        _dbName = "admin-funds-snap-" + Guid.NewGuid().ToString("N");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = AdminKey,
                ["Funds:SchedulerEnabled"] = "false",
                ["Funds:Cache:WarmupOnStartup"] = "false",
                ["Redis:Host"] = ""
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
                    o.UseInMemoryDatabase(_dbName);
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

        var cat1 = new FundCategory
        {
            Slug = "equity-growth",
            NameAr = "نمو الأسهم",
            NameEn = "Equity Growth",
            Icon = "trending-up",
            Color = "#10B981",
            IsAutoDerived = false,
            IsIndexTracking = false,
            AutoRuleJson = "",
            IsVisible = true,
            DisplayOrder = 1,
            IsDeleted = false,
            DeletedReason = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        var cat2 = new FundCategory
        {
            Slug = "tech-funds",
            NameAr = "صناديق التكنولوجيا",
            NameEn = "Technology Funds",
            Icon = "cpu",
            Color = "#3B82F6",
            IsAutoDerived = false,
            IsIndexTracking = false,
            AutoRuleJson = "",
            IsVisible = true,
            DisplayOrder = 2,
            IsDeleted = false,
            DeletedReason = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        _db.FundCategories.AddRange(cat1, cat2);

        var fund = new Fund
        {
            SndukId = 2001,
            PublicId = "snap-admin-f001",
            Slug = "snap-admin-f001",
            NameAr = "صندوق الرائد للأسهم",
            NameEn = "Al Raed Equity Fund",
            Code = "RD001",
            Type = FundType.Equity,
            IsActive = true,
            IsIslamic = true,
            Aum = 5000000m,
            ManagementFee = 1.25m,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Funds.Add(fund);

        var syncLog = new FundSyncLog
        {
            TriggeredBy = "Manual",
            Status = FundSyncStatus.Success,
            TotalFunds = 10,
            UpdatedFunds = 10,
            FailedFunds = 0,
            SkippedFunds = 0,
            ErrorSummary = "",
            RunAt = DateTime.UtcNow.AddHours(-1),
            FinishedAt = DateTime.UtcNow.AddMinutes(-50)
        };
        _db.FundSyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        var syncDetail = new FundSyncLogDetail
        {
            FundSyncLogId = syncLog.Id,
            FundId = fund.Id,
            FundCode = fund.Code,
            Status = FundSyncDetailStatus.Success,
            ErrorMessage = "Successfully synced details",
            AttemptedAt = DateTime.UtcNow.AddMinutes(-55)
        };
        _db.FundSyncLogDetails.Add(syncDetail);

        var changeLog = new FundCategoryChangeLog
        {
            FundId = fund.Id,
            CategoryId = cat1.Id,
            Action = CategoryChangeAction.ManualIncluded,
            PerformedBy = "admin",
            Reason = "Initial test seed",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        _db.FundCategoryChangeLogs.Add(changeLog);

        await _db.SaveChangesAsync();

        _catId1 = cat1.Id;
        _catId2 = cat2.Id;
        _fundId = fund.Id;
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", AdminKey);
        return client;
    }

    [Fact]
    public async Task GetSyncSettings_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/sync/settings");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetSyncSettings", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UpdateSyncSettings_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            IsEnabled = true,
            PreventOverlap = true,
            RequestDelaySeconds = 2,
            MaxRetries = 4,
            RetryBackoffSeconds = 10,
            AutoIndexFundTypeEnabled = true,
            AutoClassificationEnabled = false
        };
        var res = await client.PatchAsJsonAsync("/api/admin/funds/sync/settings", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_UpdateSyncSettings", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetLatestSyncRun_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/sync/latest");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetLatestSyncRun", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetSyncHistory_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/sync/history?page=1&pageSize=20");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetSyncHistory", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetSyncRunDetails_Returns200()
    {
        var client = CreateAdminClient();
        var syncLog = await _db!.FundSyncLogs.FirstAsync();
        var res = await client.GetAsync($"/api/admin/funds/sync/{syncLog.Id}/details");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetSyncRunDetails", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task BackfillLogos_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync("/api/admin/funds/logos/backfill", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_BackfillLogos", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetAllCategories_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/categories?includeDeleted=false");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetAllCategories", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetCategory_Existing_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/categories/{_catId1}");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetCategory_Existing", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetCategory_NotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/categories/99999");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetCategory_NotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateCategory_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "صناديق الصكوك",
            NameEn = "Sukuk Funds",
            Slug = "sukuk-funds",
            Icon = "shield",
            Color = "#059669",
            IsAutoDerived = false,
            IsIndexTracking = false,
            AutoRuleJson = "",
            IsVisible = true,
            DisplayOrder = 3
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/categories", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_CreateCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UpdateCategory_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "صناديق التكنولوجيا المحدثة",
            NameEn = "Updated Tech Funds",
            DisplayOrder = 10
        };
        var res = await client.PatchAsJsonAsync($"/api/admin/funds/categories/{_catId2}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_UpdateCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteCategory_WithFunds_NoConfirm_Returns400()
    {
        var client = CreateAdminClient();
        // Include fund in cat1 first
        await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);

        var res = await client.DeleteAsync($"/api/admin/funds/categories/{_catId1}?confirm=false");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_DeleteCategory_NoConfirm", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteCategory_Confirmed_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/categories/{_catId1}?confirm=true");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_DeleteCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RestoreCategory_Returns200()
    {
        var client = CreateAdminClient();
        // Delete first
        await client.DeleteAsync($"/api/admin/funds/categories/{_catId1}?confirm=true");

        var res = await client.PostAsync($"/api/admin/funds/categories/{_catId1}/restore", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_RestoreCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MergeCategories_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            SourceCategoryId = _catId2,
            TargetCategoryId = _catId1
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/categories/merge", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_MergeCategories_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetFundCategories_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/funds/{_fundId}/categories");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetFundCategories", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task IncludeFundInCategory_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_IncludeFundInCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ExcludeFundFromCategory_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/exclude", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_ExcludeFundFromCategory_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ResetFundCategoryOverride_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);
        var res = await client.DeleteAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/override");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_ResetFundCategoryOverride_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetFundCategoryOverrides_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/funds/{_fundId}/category-overrides");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetFundCategoryOverrides", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetCategoryFunds_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);
        var res = await client.GetAsync($"/api/admin/funds/categories/{_catId1}/funds");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetCategoryFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ResetFundOverrides_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);
        var res = await client.DeleteAsync($"/api/admin/funds/funds/{_fundId}/overrides");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_ResetFundOverrides", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ResetCategoryOverrides_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/funds/{_fundId}/categories/{_catId1}/include", null);
        var res = await client.DeleteAsync($"/api/admin/funds/categories/{_catId1}/overrides");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_ResetCategoryOverrides", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetCategoryChanges_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/category-changes?page=1&pageSize=50");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetCategoryChanges", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetAllFunds_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/funds?page=1&pageSize=50");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetAllFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetFund_Existing_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/funds/{_fundId}");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetFund_Existing", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetFund_NotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/funds/99999");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Funds_GetFund_NotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task FetchMissingPricesForFund_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient(); // No X-Admin-Key header
        var res = await client.PostAsync($"/api/admin/funds/funds/{_fundId}/fetch-missing-prices", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
