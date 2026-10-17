using System.Net;
using System.Net.Http.Json;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Tests.Admin;

public class AdminStocksManagementSnapshotTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private const string AdminKey = "admin-stocks-snap-test-key";
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _stockId;
    private int _sectorId;
    private int _shariahIndexId;
    private int _sectoralIndexId;

    public AdminStocksManagementSnapshotTests()
    {
        _dbName = "admin-stocks-snap-" + Guid.NewGuid().ToString("N");
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

        var sector = new Sector
        {
            NameAr = "بنوك",
            NameEn = "Banks"
        };
        _db.Sectors.Add(sector);

        var shariahIndex = new Index
        {
            Code = "EGX33",
            NameAr = "مؤشر الشريعة",
            NameEn = "Shariah Index"
        };
        var sectoralIndex = new Index
        {
            Code = "SECTORAL-INDICES",
            NameAr = "مؤشرات قطاعية",
            NameEn = "Sectoral Indices"
        };
        _db.Indices.AddRange(shariahIndex, sectoralIndex);

        var stock = new Stock
        {
            Ticker = "COMI",
            NameAr = "البنك التجاري الدولي",
            NameEn = "Commercial International Bank",
            Sector = sector,
            IsManual = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Stocks.Add(stock);

        await _db.SaveChangesAsync();

        _stockId = stock.Id;
        _sectorId = sector.Id;
        _shariahIndexId = shariahIndex.Id;
        _sectoralIndexId = sectoralIndex.Id;
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

    // 1. Search
    [Fact]
    public async Task Search_NoQuery_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/stocks-management/search");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Search_NoQuery", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task Search_WithQuery_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/stocks-management/search?q=comi");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Search_WithQuery", (int)res.StatusCode, content);
    }

    // 2. Create stock
    [Fact]
    public async Task CreateStock_Success_Returns201()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Ticker = "fawry",
            NameAr = "فوري",
            NameEn = "Fawry",
            SectorId = _sectorId
        };
        var res = await client.PostAsJsonAsync("/api/admin/stocks-management", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Create_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateStock_EmptyTicker_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Ticker = "   ",
            NameAr = "فوري"
        };
        var res = await client.PostAsJsonAsync("/api/admin/stocks-management", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Create_EmptyTicker", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateStock_DuplicateTicker_Returns409()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Ticker = "COMI",
            NameAr = "تجاري دولي مكرر"
        };
        var res = await client.PostAsJsonAsync("/api/admin/stocks-management", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Create_DuplicateTicker", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task CreateStock_InvalidSector_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Ticker = "NEWTICK",
            SectorId = 99999
        };
        var res = await client.PostAsJsonAsync("/api/admin/stocks-management", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_Create_InvalidSector", (int)res.StatusCode, content);
    }

    // 3. Assign / move sector
    [Fact]
    public async Task AssignSector_Success_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new { SectorId = _sectorId };
        var res = await client.PatchAsJsonAsync($"/api/admin/stocks-management/{_stockId}/sector", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AssignSector_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AssignSector_StockNotFound_Returns404()
    {
        var client = CreateAdminClient();
        var payload = new { SectorId = _sectorId };
        var res = await client.PatchAsJsonAsync("/api/admin/stocks-management/99999/sector", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AssignSector_StockNotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AssignSector_SectorNotFound_Returns400()
    {
        var client = CreateAdminClient();
        var payload = new { SectorId = 99999 };
        var res = await client.PatchAsJsonAsync($"/api/admin/stocks-management/{_stockId}/sector", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AssignSector_SectorNotFound", (int)res.StatusCode, content);
    }

    // 4. Remove sector
    [Fact]
    public async Task RemoveFromSector_Success_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/stocks-management/{_stockId}/sector");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_RemoveFromSector_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RemoveFromSector_StockNotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync("/api/admin/stocks-management/99999/sector");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_RemoveFromSector_StockNotFound", (int)res.StatusCode, content);
    }

    // 5. Pickers
    [Fact]
    public async Task GetAllSectors_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/stocks-management/sectors");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_GetAllSectors", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task GetAllIndices_Returns200()
    {
        var client = CreateAdminClient();
        var res = await client.GetAsync("/api/admin/stocks-management/indices");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_GetAllIndices", (int)res.StatusCode, content);
    }

    // 6. Index constituent management
    [Fact]
    public async Task AddToIndex_Success_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Weight = 5.25m,
            EffectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var res = await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/{_shariahIndexId}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AddToIndex_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AddToIndex_AlreadyConstituent_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Weight = 5.25m,
            EffectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/{_shariahIndexId}", payload);

        // Second time should return the existing row
        var res = await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/{_shariahIndexId}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AddToIndex_AlreadyConstituent", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AddToIndex_SectoralIndex_NullsWeight_Returns200()
    {
        var client = CreateAdminClient();
        var payload = new
        {
            Weight = 8.5m,
            EffectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var res = await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/{_sectoralIndexId}", payload);
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AddToIndex_SectoralIndex_NullsWeight", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AddToIndex_StockNotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsJsonAsync($"/api/admin/stocks-management/99999/indices/{_shariahIndexId}", new { });
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AddToIndex_StockNotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task AddToIndex_IndexNotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/99999", new { });
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_AddToIndex_IndexNotFound", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RemoveFromIndex_Success_Returns204()
    {
        var client = CreateAdminClient();
        // First add
        await client.PostAsJsonAsync($"/api/admin/stocks-management/{_stockId}/indices/{_shariahIndexId}", new { Weight = 1.0m });

        // Remove
        var res = await client.DeleteAsync($"/api/admin/stocks-management/{_stockId}/indices/{_shariahIndexId}");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_RemoveFromIndex_Success", (int)res.StatusCode, content);
    }

    [Fact]
    public async Task RemoveFromIndex_NotFound_Returns404()
    {
        var client = CreateAdminClient();
        var res = await client.DeleteAsync($"/api/admin/stocks-management/{_stockId}/indices/99999");
        var content = await res.Content.ReadAsStringAsync();
        SnapshotAssert.MatchesStoredSnapshot("Admin_StocksManagement_RemoveFromIndex_NotFound", (int)res.StatusCode, content);
    }
}
