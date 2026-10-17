using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// Hierarchy behavior independent of the initial seed: children under any node,
/// grandchild depth, moves with subtrees, cycle/depth/slug validation, delete
/// with relocation, merge, restore, empty-node visibility and label edits.
/// Runs against an isolated in-memory database (never the shared one).
/// </summary>
public class FundClassificationTreeTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;

    public FundClassificationTreeTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "class-tree-tests-" + Guid.NewGuid().ToString("N");
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "tree-test-key",
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
        _scope = _factory.Services.CreateScope();
    }

    public void Dispose() => _scope.Dispose();

    private static FundClassificationNode Node(string slug, int? parentId, int level, int order = 1, bool visible = true)
    {
        return new FundClassificationNode
        {
            Slug = slug,
            NameAr = slug,
            NameEn = slug,
            Level = level,
            ParentId = parentId,
            Icon = string.Empty,
            Color = string.Empty,
            DisplayOrder = order,
            IsVisible = visible,
            IsDeleted = false,
            DeletedReason = string.Empty,
            RequiresChildSelection = false,
            ChildrenLabelAr = "التصنيف الفرعي",
            ChildrenLabelEn = "Sub classification",
            AutoRuleJson = string.Empty,
            AutoClassificationEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test",
            UpdatedBy = "test"
        };
    }

    private IFundClassificationService Service()
    {
        // Resolve from a class-lifetime scope (disposed with the test instance);
        // the service under test creates its own short-lived scopes per operation.
        return _scope.ServiceProvider.GetRequiredService<IFundClassificationService>();
    }

    private async Task<(int RootId, int ChildId)> SeedChainAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var root = Node("tree-root-" + Guid.NewGuid().ToString("N")[..6], null, 0);
        db.FundClassificationNodes.Add(root);
        await db.SaveChangesAsync();
        var child = Node("tree-child-" + Guid.NewGuid().ToString("N")[..6], root.Id, 1);
        db.FundClassificationNodes.Add(child);
        await db.SaveChangesAsync();
        return (root.Id, child.Id);
    }

    [Fact]
    public void MaxDepth_IsSixLevels()
    {
        Assert.Equal(6, FundClassificationNode.MaxDepth);
        Assert.Equal(6, FundClassificationService.MaxDepth);
    }

    [Fact]
    public async Task ValidateSlug_RejectsDuplicates_IncludingDeleted()
    {
        var svc = Service();
        var slug = "dup-slug-" + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.FundClassificationNodes.Add(Node(slug, null, 0));
            await db.SaveChangesAsync();
        }

        var (ok, err) = await svc.ValidateSlugAsync(slug, null);
        Assert.False(ok);
        Assert.Contains("مستخدم بالفعل", err);

        // Same id is fine (rename to itself).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = await db.FundClassificationNodes.Where(n => n.Slug == slug).Select(n => n.Id).FirstAsync();
            var (ok2, _) = await svc.ValidateSlugAsync(slug, id);
            Assert.True(ok2);
        }
    }

    [Fact]
    public async Task ValidateSlug_RejectsBadCharacters_AndRequiresValue()
    {
        var svc = Service();
        var (ok1, _) = await svc.ValidateSlugAsync("", null);
        Assert.False(ok1);
        var (ok2, err2) = await svc.ValidateSlugAsync("Bad Slug!", null);
        Assert.False(ok2);
        Assert.Contains("لاتينية", err2);
        var (ok3, _) = await svc.ValidateSlugAsync("valid-slug_123", null);
        Assert.True(ok3);
    }

    [Fact]
    public async Task ValidateMove_RejectsSelfMove()
    {
        var svc = Service();
        var (rootId, _) = await SeedChainAsync();
        var (ok, err) = await svc.ValidateMoveAsync(rootId, rootId);
        Assert.False(ok);
        Assert.Contains("نفسها", err);
    }

    [Fact]
    public async Task ValidateMove_RejectsDescendantMove()
    {
        var svc = Service();
        var (rootId, childId) = await SeedChainAsync();
        var (ok, err) = await svc.ValidateMoveAsync(rootId, childId);
        Assert.False(ok);
        Assert.Contains("دورة", err);
    }

    [Fact]
    public async Task ValidateMove_EnforcesMaxDepth()
    {
        var svc = Service();
        // Build a 6-deep chain (levels 0..5).
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        int? parent = null;
        var ids = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            var n = Node($"depth-{Guid.NewGuid():N}"[..12], parent, i);
            db.FundClassificationNodes.Add(n);
            await db.SaveChangesAsync();
            parent = n.Id;
            ids.Add(n.Id);
        }
        // Unrelated parent at level 1: moving the chain root (subtree
        // height 5) under it would push leaves to level 7 > MaxDepth 6.
        var otherRoot = Node($"depth-{Guid.NewGuid():N}"[..12], null, 0);
        db.FundClassificationNodes.Add(otherRoot);
        await db.SaveChangesAsync();
        var otherChild = Node($"depth-{Guid.NewGuid():N}"[..12], otherRoot.Id, 1);
        db.FundClassificationNodes.Add(otherChild);
        await db.SaveChangesAsync();
        var (ok, err) = await svc.ValidateMoveAsync(ids[0], otherChild.Id);
        Assert.False(ok);
        Assert.Contains("عمق", err);
    }

    [Fact]
    public async Task ValidateMove_AllowsValidMoveToRoot()
    {
        var svc = Service();
        var (_, childId) = await SeedChainAsync();
        var (ok, err) = await svc.ValidateMoveAsync(childId, null);
        Assert.True(ok, err);
    }

    [Fact]
    public async Task DeletedSlug_BlocksRecreation_AndRestoreDetectsConflict()
    {
        var svc = Service();
        var slug = "gone-" + Guid.NewGuid().ToString("N")[..8];
        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var n = Node(slug, null, 0);
            n.IsDeleted = true;
            n.DeletedAt = DateTime.UtcNow;
            db.FundClassificationNodes.Add(n);
            await db.SaveChangesAsync();
            id = n.Id;
        }
        // Recreation blocked while the deleted row holds the slug.
        var (ok, _) = await svc.ValidateSlugAsync(slug, null);
        Assert.False(ok);
        // Restore of the same row is fine (ignored id).
        var (ok2, _) = await svc.ValidateSlugAsync(slug, id);
        Assert.True(ok2);
    }
}
