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

public class AdminFundClassificationSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private const string AdminKey = "admin-snap-test-key";
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _rootNodeId;
    private int _childNodeId;
    private int _fundId;

    public AdminFundClassificationSnapshotTests()
    {
        _dbName = "admin-class-snap-" + Guid.NewGuid().ToString("N");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = AdminKey,
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

        var root = new FundClassificationNode
        {
            Slug = "equity",
            NameAr = "أسهم",
            NameEn = "Equity",
            Level = 0,
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            RequiresChildSelection = true,
            ChildrenLabelAr = "التصنيف الفرعي",
            ChildrenLabelEn = "Sub-classification",
            AutoRuleJson = "",
            AutoClassificationEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        _db.FundClassificationNodes.Add(root);
        await _db.SaveChangesAsync();
        _rootNodeId = root.Id;

        var child = new FundClassificationNode
        {
            Slug = "active",
            NameAr = "نشط",
            NameEn = "Active",
            Level = 1,
            ParentId = _rootNodeId,
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            RequiresChildSelection = false,
            ChildrenLabelAr = "",
            ChildrenLabelEn = "",
            AutoRuleJson = "",
            AutoClassificationEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        _db.FundClassificationNodes.Add(child);

        var fund = new Fund
        {
            SndukId = 1001,
            PublicId = "snap-f001",
            Slug = "snap-f001",
            NameAr = "صندوق الأسهم النشط",
            NameEn = "Active Equity Fund",
            Code = "F001",
            Type = FundType.Equity,
            IsActive = true,
            IsIslamic = true,
            Aum = 1000000m,
            ManagementFee = 1.5m,
            SndukCreatedAt = DateTime.UtcNow,
            SndukUpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Funds.Add(fund);
        await _db.SaveChangesAsync();

        _childNodeId = child.Id;
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
    public async Task SeedMigrate_Returns200_WithReport()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync("/api/admin/funds/classification/seed-migrate", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_SeedMigrate", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetTree_Returns200_WithNodes()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/classification/tree");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_GetTree", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateNode_Valid_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "دخل ثابت",
            NameEn = "Fixed Income",
            Slug = "fixed-income-new",
            DisplayOrder = 2
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/classification/nodes", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_CreateNode_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateNode_DuplicateSlug_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "أسهم مكررة",
            NameEn = "Equity Duplicate",
            Slug = "equity"
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/classification/nodes", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_CreateNode_DuplicateSlug", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UpdateNode_Valid_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "نشط معدل",
            NameEn = "Active Updated",
            DisplayOrder = 5
        };
        var res = await client.PatchAsJsonAsync($"/api/admin/funds/classification/nodes/{_childNodeId}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_UpdateNode_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MoveNode_Cycle_Returns400()
    {
        var client = CreateAdminClient();
        // Moving root under its child is a cycle
        var payload = new { NewParentId = _childNodeId };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/classification/nodes/{_rootNodeId}/move", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_MoveNode_Cycle", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MergeNodes_TargetInsideSourceSubtree_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new { SourceId = _rootNodeId, TargetId = _childNodeId };
        var res = await client.PostAsJsonAsync("/api/admin/funds/classification/nodes/merge", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_MergeNodes_Cycle", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeletePreview_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/classification/nodes/{_rootNodeId}/delete-preview");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_DeletePreview", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteNode_WithoutConfirm_Returns400()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/classification/nodes/{_rootNodeId}?confirm=false");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_DeleteNode_NoConfirm", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteNode_Confirmed_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/classification/nodes/{_childNodeId}?confirm=true&resetFundsToAuto=true");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_DeleteNode_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RestoreNode_Returns200()
    {
        var client = CreateAdminClient();
        // Delete child first
        await client.DeleteAsync($"/api/admin/funds/classification/nodes/{_childNodeId}?confirm=true&resetFundsToAuto=true");

        var res = await client.PostAsync($"/api/admin/funds/classification/nodes/{_childNodeId}/restore", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_RestoreNode_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetNodeFunds_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/classification/nodes/{_childNodeId}/funds");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_GetNodeFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task BulkAddFunds_Returns200_WithAddedAndFailed()
    {
        var client = CreateAdminClient();
        var payload = new { FundIds = new List<int> { _fundId, 999999 } };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/classification/nodes/{_childNodeId}/funds/bulk", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_BulkAddFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task BulkRemoveFunds_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new { FundIds = new List<int> { _fundId }, ResetToAuto = true };
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/funds/classification/nodes/{_childNodeId}/funds/bulk")
        {
            Content = JsonContent.Create(payload)
        };
        var res = await client.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_BulkRemoveFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AssignFund_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new { NodeId = _childNodeId, Reason = "Manual snapshot assignment" };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/classification/funds/{_fundId}/assign", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_AssignFund_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ResetFundAssign_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/classification/funds/{_fundId}/assign");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_ResetFundAssign_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetReview_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/classification/review");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_GetReview", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task Reclassify_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync("/api/admin/funds/classification/reclassify-unclassified", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_Reclassify", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetChanges_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/classification/changes");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_GetChanges", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetBackup_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/classification/backup");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_Classification_GetBackup", (int)res.StatusCode, content);
    }
}
