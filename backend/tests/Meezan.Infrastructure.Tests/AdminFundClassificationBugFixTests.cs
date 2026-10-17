using System.Net.Http.Json;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Meezan.Infrastructure.Tests.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Meezan.Infrastructure.Tests;

public class AdminFundClassificationBugFixTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _dbName;
    private const string AdminKey = "admin-bug-test-key";
    private IServiceScope? _scope;
    private ApplicationDbContext? _db;

    private int _root1Id;
    private int _child1Id;
    private int _root2Id;
    private int _fund1Id;
    private int _fund2Id;

    public AdminFundClassificationBugFixTests()
    {
        _dbName = "admin-class-bug-" + Guid.NewGuid().ToString("N");
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

        var oldTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var root1 = new FundClassificationNode
        {
            Slug = "root-one",
            NameAr = "جذر أول",
            NameEn = "Root One",
            Level = 0,
            DisplayOrder = 1,
            IsVisible = true,
            CreatedAt = oldTime,
            UpdatedAt = oldTime,
            CreatedBy = "seed",
            UpdatedBy = "seed"
        };
        var root2 = new FundClassificationNode
        {
            Slug = "root-two",
            NameAr = "جذر ثان",
            NameEn = "Root Two",
            Level = 0,
            DisplayOrder = 2,
            IsVisible = true,
            CreatedAt = oldTime,
            UpdatedAt = oldTime,
            CreatedBy = "seed",
            UpdatedBy = "seed"
        };
        _db.FundClassificationNodes.AddRange(root1, root2);
        await _db.SaveChangesAsync();

        var child1 = new FundClassificationNode
        {
            Slug = "child-one",
            NameAr = "ابن أول",
            NameEn = "Child One",
            ParentId = root1.Id,
            Level = 1,
            DisplayOrder = 1,
            IsVisible = true,
            CreatedAt = oldTime,
            UpdatedAt = oldTime,
            CreatedBy = "seed",
            UpdatedBy = "seed"
        };
        _db.FundClassificationNodes.Add(child1);

        var fund1 = new Fund
        {
            SndukId = 2001,
            PublicId = "bug-f001",
            Slug = "bug-f001",
            NameAr = "صندوق 1",
            NameEn = "Fund 1",
            Code = "BF01",
            Type = FundType.Equity,
            IsActive = true,
            CreatedAt = oldTime,
            UpdatedAt = oldTime
        };
        var fund2 = new Fund
        {
            SndukId = 2002,
            PublicId = "bug-f002",
            Slug = "bug-f002",
            NameAr = "صندوق 2",
            NameEn = "Fund 2",
            Code = "BF02",
            Type = FundType.Equity,
            IsActive = true,
            CreatedAt = oldTime,
            UpdatedAt = oldTime
        };
        _db.Funds.AddRange(fund1, fund2);
        await _db.SaveChangesAsync();

        _root1Id = root1.Id;
        _root2Id = root2.Id;
        _child1Id = child1.Id;
        _fund1Id = fund1.Id;
        _fund2Id = fund2.Id;
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
    public async Task MoveNode_UpdatesUpdatedAt_OnlyOnMovedSubtree_NotOnUnrelatedNodes()
    {
        var client = CreateAdminClient();

        // Move child1 from root1 to root2
        var payload = new { NewParentId = _root2Id };
        var res = await client.PostAsJsonAsync($"/api/admin/funds/classification/nodes/{_child1Id}/move", payload);
        res.EnsureSuccessStatusCode();

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unmovedRoot1 = await db.FundClassificationNodes.FindAsync(_root1Id);
        var unmovedRoot2 = await db.FundClassificationNodes.FindAsync(_root2Id);
        var movedChild = await db.FundClassificationNodes.FindAsync(_child1Id);

        var oldTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // child1 was moved -> UpdatedAt was updated
        Assert.True(movedChild!.UpdatedAt > oldTime);
        Assert.Equal("admin", movedChild.UpdatedBy);

        // root1 and root2 were NOT in the moved subtree -> UpdatedAt must remain unchanged
        Assert.Equal(oldTime, unmovedRoot1!.UpdatedAt);
        Assert.Equal("seed", unmovedRoot1.UpdatedBy);
        Assert.Equal(oldTime, unmovedRoot2!.UpdatedAt);
        Assert.Equal("seed", unmovedRoot2.UpdatedBy);
    }

    [Fact]
    public async Task BulkAdd_And_BulkRemove_AreAtomic_WithExactResponseShape()
    {
        var client = CreateAdminClient();

        // Bulk Add: fund1 exists, 999999 does not exist
        var addPayload = new { FundIds = new List<int> { _fund1Id, 999999 } };
        var addRes = await client.PostAsJsonAsync($"/api/admin/funds/classification/nodes/{_child1Id}/funds/bulk", addPayload);
        addRes.EnsureSuccessStatusCode();

        var addResult = await addRes.Content.ReadFromJsonAsync<BulkAddFundsResultDto>();
        Assert.NotNull(addResult);
        Assert.Equal(1, addResult.Added);
        Assert.Equal(1, addResult.Failed);
        Assert.Single(addResult.Errors);

        // Bulk Remove: reset fund1 to auto
        var removePayload = new { FundIds = new List<int> { _fund1Id }, ResetToAuto = true };
        var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/funds/classification/nodes/{_child1Id}/funds/bulk")
        {
            Content = JsonContent.Create(removePayload)
        };
        var removeRes = await client.SendAsync(req);
        removeRes.EnsureSuccessStatusCode();

        var removeResult = await removeRes.Content.ReadFromJsonAsync<BulkRemoveFundsResultDto>();
        Assert.NotNull(removeResult);
        Assert.Equal(0, removeResult.Moved);
        Assert.Equal(1, removeResult.Reset);
        Assert.Equal(0, removeResult.Failed);
        Assert.Empty(removeResult.Errors);
    }
}
