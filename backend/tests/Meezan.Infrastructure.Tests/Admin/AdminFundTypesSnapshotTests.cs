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

public class AdminFundTypesSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private const string AdminKey = "admin-snap-test-key";
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _typeId1;
    private int _typeId2;
    private int _fundId;

    public AdminFundTypesSnapshotTests()
    {
        _dbName = "admin-types-snap-" + Guid.NewGuid().ToString("N");
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

        var type1 = new FundTypeDefinition
        {
            Slug = "equity",
            NameAr = "أسهم",
            NameEn = "Equity",
            Icon = "chart",
            Color = "#10B981",
            DisplayOrder = 1,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        var type2 = new FundTypeDefinition
        {
            Slug = "fixed-income",
            NameAr = "دخل ثابت",
            NameEn = "Fixed Income",
            Icon = "wallet",
            Color = "#3B82F6",
            DisplayOrder = 2,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };
        _db.FundTypeDefinitions.AddRange(type1, type2);

        var fund = new Fund
        {
            SndukId = 3001,
            PublicId = "snap-type-f001",
            Slug = "snap-type-f001",
            NameAr = "صندوق الأسهم الرائد",
            NameEn = "Pioneer Equity Fund",
            Code = "PE001",
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
        await _db.SaveChangesAsync();

        var changeLog = new FundTypeChangeLog
        {
            FundId = fund.Id,
            FundTypeDefinitionId = type1.Id,
            Action = FundTypeChangeAction.Created,
            PerformedBy = "admin",
            Reason = "Initial test seed",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        _db.FundTypeChangeLogs.Add(changeLog);

        await _db.SaveChangesAsync();

        _typeId1 = type1.Id;
        _typeId2 = type2.Id;
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
    public async Task GetAllTypes_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/types?includeDeleted=false");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_GetAll", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetById_Existing_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/types/{_typeId1}");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_GetById_Existing", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/types/99999");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_GetById_NotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateType_Success_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "صناديق نقدية",
            NameEn = "Money Market",
            Slug = "money-market",
            Icon = "dollar",
            Color = "#F59E0B",
            DisplayOrder = 3,
            IsVisible = true
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/types", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Create_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateType_DuplicateSlug_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "أسهم مكررة",
            NameEn = "Equity Duplicate",
            Slug = "equity"
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/types", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Create_DuplicateSlug", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task UpdateType_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            NameAr = "أسهم محدثة",
            NameEn = "Updated Equity",
            DisplayOrder = 5,
            IsVisible = true
        };
        var res = await client.PatchAsJsonAsync($"/api/admin/funds/types/{_typeId1}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Update_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteType_WithFunds_NoConfirm_Returns400()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/types/{_typeId1}?confirm=false");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Delete_NoConfirm", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task DeleteType_Confirmed_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/funds/types/{_typeId1}?confirm=true");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Delete_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RestoreType_Returns200()
    {
        var client = CreateAdminClient();
        await client.DeleteAsync($"/api/admin/funds/types/{_typeId1}?confirm=true");
        var res = await client.PostAsync($"/api/admin/funds/types/{_typeId1}/restore", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Restore_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MergeTypes_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            SourceId = _typeId2,
            TargetId = _typeId1
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/types/merge", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Merge_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ReorderTypes_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            OrderedIds = new List<int> { _typeId2, _typeId1 }
        };
        var res = await client.PostAsJsonAsync("/api/admin/funds/types/reorder", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_Reorder_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetTypeFunds_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync($"/api/admin/funds/types/{_typeId1}/funds?page=1&pageSize=50");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_GetTypeFunds", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task SetFundType_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsync($"/api/admin/funds/types/funds/{_fundId}/{_typeId2}", null);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_SetFundType_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task BulkAddFundsToType_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            FundIds = new List<int> { _fundId }
        };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/types/{_typeId2}/funds/bulk", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_BulkAddFunds_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MoveFundType_WithTarget_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            TargetTypeId = (int?)_typeId2
        };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/types/funds/{_fundId}/move", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_MoveFundType_Target_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task MoveFundType_ResetToAuto_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/types/funds/{_fundId}/{_typeId2}", null);
        var payload = new
        {
            TargetTypeId = (int?)null
        };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/types/funds/{_fundId}/move", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_MoveFundType_Reset_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task ClearFundType_Returns200()
    {
        var client = CreateAdminClient();
        await client.PostAsync($"/api/admin/funds/types/funds/{_fundId}/{_typeId2}", null);
        var res = await client.DeleteAsync($"/api/admin/funds/types/funds/{_fundId}");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_ClearFundType_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetChanges_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/funds/types/changes?page=1&pageSize=50");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_FundTypes_GetChanges", (int)res.StatusCode, content);
    }
}
