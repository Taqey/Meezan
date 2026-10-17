using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Meezan.Infrastructure.Services;

/// <summary>
/// Loads the detail page in parallel across independent DbContexts.
/// The sequential version issued ~7 round-trips to a REMOTE SQL Server
/// (~130 ms each), which pushed the page past its 1 s budget. A DbContext is not
/// thread-safe, so each branch gets its own scope.
/// </summary>
public sealed class FundDetailQuery : IFundDetailQuery
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FundDetailQuery(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private async Task<T> InScopeAsync<T>(Func<ApplicationDbContext, Task<T>> work, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await work(db);
    }

    public async Task<FundDetailBundle?> LoadAsync(int fundId, CancellationToken ct = default)
    {
        var fundTask = InScopeAsync(db => db.Funds.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fundId, ct), ct);

        var pricesTask = InScopeAsync(db => db.FundPriceHistories.AsNoTracking()
            .Where(p => p.FundId == fundId)
            .OrderBy(p => p.Date)
            .ToListAsync(ct), ct);

        var analysisTask = InScopeAsync(db => db.FundAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.FundId == fundId, ct), ct);

        var similarsTask = InScopeAsync(db => db.FundSimilars.AsNoTracking()
            .Include(s => s.SimilarFund)
            .Where(s => s.FundId == fundId)
            .ToListAsync(ct), ct);

        var overridesTask = InScopeAsync(db => db.FundCategoryOverrides.AsNoTracking()
            .Where(o => o.FundId == fundId)
            .ToListAsync(ct), ct);

        var categoriesTask = InScopeAsync(db => db.FundCategories.AsNoTracking()
            .Where(c => c.IsVisible && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.NameAr)
            .ToListAsync(ct), ct);

        var typeDefsTask = InScopeAsync(db => db.FundTypeDefinitions.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .ToListAsync(ct), ct);

        await Task.WhenAll(fundTask, pricesTask, analysisTask, similarsTask,
            overridesTask, categoriesTask, typeDefsTask);

        var fund = await fundTask;
        if (fund == null) return null;

        // The fund type override is a single small row; fetch it after the fan-out.
        var typeOverrides = await InScopeAsync(db => db.FundTypeOverrides.AsNoTracking()
            .Where(o => o.FundId == fundId)
            .ToListAsync(ct), ct);

        return new FundDetailBundle
        {
            Fund = fund,
            PriceHistory = await pricesTask,
            Analysis = await analysisTask,
            SimilarFunds = await similarsTask,
            Overrides = await overridesTask,
            Categories = await categoriesTask,
            TypeDefinitions = await typeDefsTask,
            TypeOverrides = typeOverrides
        };
    }

    public async Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var nodeRepo = sp.GetRequiredService<IFundClassificationNodeRepository>();
        var overrideRepo = sp.GetRequiredService<IFundClassificationOverrideRepository>();
        var settingsRepo = sp.GetRequiredService<IFundSyncSettingsRepository>();
        var db = sp.GetRequiredService<Meezan.Infrastructure.Persistence.ApplicationDbContext>();

        var nodes = await nodeRepo.GetAllAsync(true, ct);
        var settings = await settingsRepo.GetAsync(ct);
        var overrides = await db.FundClassificationOverrides.AsNoTracking().ToListAsync(ct);

        var byId = nodes.ToDictionary(n => n.Id);
        var bySlug = nodes.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);

        return new ClassificationContext
        {
            Nodes = nodes,
            ById = byId,
            BySlug = bySlug,
            OverridesByFund = overrides.GroupBy(o => o.FundId).ToDictionary(g => g.Key, g => g.ToList()),
            AutoEnabled = settings?.AutoClassificationEnabled ?? true
        };
    }

    public async Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Fund> funds, ClassificationContext ctx)
    {
        var dict = new Dictionary<int, EffectiveClassification>();
        foreach (var f in funds) dict[f.Id] = FundClassificationService.ResolveEffective(f, ctx);
        return dict;
    }
}