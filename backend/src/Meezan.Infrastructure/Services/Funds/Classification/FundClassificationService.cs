using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Funds.Classification;

public class FundClassificationService : IFundClassificationService
{
    public const int MaxDepth = FundClassificationNode.MaxDepth;
    public const string UnclassifiedSlug = "equity-unclassified";
    public const string SilverSlug = "silver";
    public const string CommoditiesSlug = "commodities";

    private readonly IServiceProvider _services;
    private readonly ILogger<FundClassificationService> _logger;

    public FundClassificationService(IServiceProvider services, ILogger<FundClassificationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public sealed record SeedNode(
        string Slug, string NameAr, string NameEn, int Level, string? ParentSlug,
        int DisplayOrder, bool IsVisible, bool RequiresChildSelection,
        string ChildrenLabelAr, string ChildrenLabelEn, string AutoRuleJson);

    /// <summary>Canonical seed. Existing rows keep their edited names/order/visibility.</summary>
    public static readonly IReadOnlyList<SeedNode> SeedTree = new List<SeedNode>
    {
        // Level 0 — top-level types (order mirrors FundTypeDefinitions seed)
        new("equity", "صندوق أسهم", "Equity Fund", 0, null, 1, true, true, "نوع صندوق الأسهم", "Equity Fund Type", ""),
        new("fixed_income", "صندوق دخل ثابت", "Fixed Income Fund", 0, null, 2, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new("fixed_income_usd", "صندوق دخل ثابت بالدولار", "USD Fixed Income Fund", 0, null, 3, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new("balanced", "صندوق متوازن", "Balanced Fund", 0, null, 4, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new("money_market", "صندوق سوق نقدي", "Money Market Fund", 0, null, 5, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new("gold", "صندوق ذهب", "Gold Fund", 0, null, 6, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new(SilverSlug, "صندوق فضة", "Silver Fund", 0, null, 7, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new("capital_preservation", "صندوق الحفاظ على رأس المال", "Capital Preservation Fund", 0, null, 8, true, false, "التصنيف الفرعي", "Sub classification", ""),
        // Level 1 — equity children
        new("sector", "صندوق قطاع", "Sector Fund", 1, "equity", 1, true, true, "القطاع", "Sector", ""),
        new("index", "صندوق مؤشر", "Index Fund", 1, "equity", 2, true, true, "المؤشر", "Index", ""),
        new("active", "صندوق نشط", "Active Fund", 1, "equity", 3, true, false, "التصنيف الفرعي", "Sub classification", ""),
        new(UnclassifiedSlug, "غير محدد", "Unclassified", 1, "equity", 99, false, false, "التصنيف الفرعي", "Sub classification", ""),
        // Level 2 — index children
        new("index-egx30", "يتبع مؤشر EGX30", "Tracks EGX30", 2, "index", 1, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["egx30","egx 30","egx-30","egx_30","إيجي إكس 30","ايجي اكس 30"],"field":"name"}"""),
        new("index-egx33", "يتبع مؤشر EGX33", "Tracks EGX33", 2, "index", 2, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["egx33","egx 33","egx-33","egx_33","إيجي إكس 33","ايجي اكس 33"],"field":"name"}"""),
        new("index-egx70", "يتبع مؤشر EGX70", "Tracks EGX70", 2, "index", 3, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["egx70","egx 70","egx-70","egx_70","إيجي إكس 70","ايجي اكس 70"],"field":"name"}"""),
        new("index-egx100", "يتبع مؤشر EGX100", "Tracks EGX100", 2, "index", 4, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["egx100","egx 100","egx-100","egx_100","إيجي إكس 100","ايجي اكس 100"],"field":"name"}"""),
        new("tracks-egx35", "يتبع مؤشر EGX35", "Tracks EGX35", 2, "index", 5, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["egx35","egx 35","egx-35","egx_35","egx35-lv","egx 35 lv","إيجي إكس 35","ايجي اكس 35"],"field":"name"}"""),
        // Level 2 — sector children
        new("sector-financial", "قطاع مالي", "Financial Sector", 2, "sector", 1, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["المالي","البنوك","financial","banks","bank"],"field":"name"}"""),
        new("sector-real-estate", "قطاع عقاري", "Real Estate Sector", 2, "sector", 2, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["العقاري","عقارات","عقاري","real estate"],"field":"name"}"""),
        new("sector-consumer", "قطاع استهلاكي", "Consumer Sector", 2, "sector", 3, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["الاستهلاكي","استهلاكي","consumer"],"field":"name"}"""),
        new("sector-industrial", "قطاع صناعي", "Industrial Sector", 2, "sector", 4, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["الصناعي","صناعي","industrial"],"field":"name"}"""),
        new("sector-technology", "قطاع تكنولوجي", "Technology Sector", 2, "sector", 5, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["التكنولوجيا","تكنولوجي","تقنية","technology","tech"],"field":"name"}"""),
        new("sector-ipo", "قطاع الاكتتابات الأولية", "IPO Sector", 2, "sector", 6, true, false, "التصنيف الفرعي", "Sub classification",
            """{"keywords":["الاكتتابات","اكتتاب","ipo"],"field":"name"}"""),
    };

    public static string MapSndukTopLevel(string? sndukType, FundType enumType)
        => FundClassificationEvaluator.MapSndukTopLevel(sndukType, enumType);

    public static string NormalizeAr(string? s)
        => FundClassificationEvaluator.NormalizeAr(s);

    public static string FundSearchText(Fund fund)
        => FundClassificationEvaluator.FundSearchText(fund);

    public static List<FundClassificationNode> BuildPath(FundClassificationNode node, Dictionary<int, FundClassificationNode> byId)
        => FundClassificationTreeEngine.BuildPath(node, byId);

    public static int ComputeLevel(FundClassificationNode node, Dictionary<int, FundClassificationNode> byId)
        => FundClassificationTreeEngine.ComputeLevel(node, byId);

    public async Task<ClassificationContext> LoadContextAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var nodeRepo = sp.GetRequiredService<IFundClassificationNodeRepository>();
        var overrideRepo = sp.GetRequiredService<IFundClassificationOverrideRepository>();
        var settingsRepo = sp.GetRequiredService<IFundSyncSettingsRepository>();
        var db = sp.GetRequiredService<Meezan.Infrastructure.Persistence.ApplicationDbContext>();

        var nodes = await nodeRepo.GetAllAsync(true, ct);
        var settings = await settingsRepo.GetAsync(ct);
        var overrides = await db.FundClassificationOverrides.AsNoTracking().ToListAsync(ct);

        var byId = nodes.ToDictionary(n => n.Id);
        var bySlug = nodes
            .GroupBy(n => n.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return new ClassificationContext
        {
            Nodes = nodes,
            ById = byId,
            BySlug = bySlug,
            OverridesByFund = overrides.GroupBy(o => o.FundId).ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToList()),
            AutoEnabled = settings?.AutoClassificationEnabled ?? true
        };
    }

    public Task<EffectiveClassification> ResolveEffectiveAsync(Fund fund, ClassificationContext ctx)
        => Task.FromResult(FundClassificationEvaluator.ResolveEffective(fund, ctx));

    public Task<Dictionary<int, EffectiveClassification>> ResolveEffectiveManyAsync(IEnumerable<Fund> funds, ClassificationContext ctx)
    {
        var dict = new Dictionary<int, EffectiveClassification>();
        foreach (var f in funds) dict[f.Id] = FundClassificationEvaluator.ResolveEffective(f, ctx);
        return Task.FromResult(dict);
    }

    public static EffectiveClassification ResolveEffective(Fund fund, ClassificationContext ctx)
        => FundClassificationEvaluator.ResolveEffective(fund, ctx);


    public async Task<List<string>> GetPathSlugsAsync(int nodeId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var all = await repo.GetAllAsync(true, ct);
        var byId = all.ToDictionary(n => n.Id);
        if (!byId.TryGetValue(nodeId, out var node)) return new List<string>();
        return BuildPath(node, byId).Select(n => n.Slug).ToList();
    }

    public async Task<List<FundClassificationNode>> GetPathAsync(int nodeId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var all = await repo.GetAllAsync(true, ct);
        var byId = all.ToDictionary(n => n.Id);
        if (!byId.TryGetValue(nodeId, out var node)) return new List<FundClassificationNode>();
        return BuildPath(node, byId);
    }

    public async Task<HashSet<int>> GetDescendantIdsAsync(int nodeId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var all = await repo.GetAllAsync(true, ct);
        return FundClassificationTreeEngine.DescendantIds(all, nodeId);
    }

    public async Task<(bool Ok, string? Error)> ValidateSlugAsync(string slug, int? ignoreId, CancellationToken ct = default)
    {
        var (ok, err) = FundSlugValidator.Validate(slug);
        if (!ok) return (ok, err);

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var all = await repo.GetAllAsync(true, ct);
        if (all.Any(n => string.Equals(n.Slug, slug.Trim(), StringComparison.OrdinalIgnoreCase) && n.Id != ignoreId))
            return (false, $"المعرف '{slug.Trim()}' مستخدم بالفعل (بما في ذلك المحذوفة).");
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ValidateMoveAsync(int nodeId, int? newParentId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var all = await repo.GetAllAsync(true, ct);
        return FundClassificationTreeEngine.ValidateMove(nodeId, newParentId, all);
    }

    // ---------- review + reclassify ----------

    public async Task<ReviewQueue> GetReviewQueueAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var fundRepo = sp.GetRequiredService<IFundRepository>();
        var ctx = await LoadContextAsync(ct);
        var funds = (await fundRepo.GetAllAsync(ct)).Where(f => f.IsActive).ToList();
        var eff = await ResolveEffectiveManyAsync(funds, ctx);
        var byId = funds.ToDictionary(f => f.Id);
        var q = new ReviewQueue();
        foreach (var (fundId, e) in eff)
        {
            if (!byId.TryGetValue(fundId, out var f)) continue;
            bool isManual = string.Equals(e.Source, "Manual", StringComparison.OrdinalIgnoreCase);
            var row = new ReviewRow
            {
                FundId = f.Id,
                Code = f.Code,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                AssetManagerName = f.AssetManagerName,
                Description = f.DescriptionAr,
                InvestmentStrategy = f.InvestmentStrategy,
                Reason = e.Reason,
                Path = e.DisplayPathAr,
                Source = e.Source,
                IsManual = isManual
            };
            if (string.Equals(e.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
                q.UnclassifiedEquity.Add(row);
            if (e.NeedsReview && e.ReviewReasons.Any(r => r.Contains("متضاربة") || r.Contains("مؤشر SNDUK") || r.Contains("مختلف")))
                q.Conflicting.Add(row);
            else if (e.NeedsReview && e.ReviewReasons.Any())
            {
                // missing-child vs conflict already handled; route remaining NeedsReview with reasons
                if (!string.Equals(e.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
                    q.MissingChild.Add(row);
            }
            else if (e.NeedsReview && string.Equals(e.Source, "SNDUK", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.BySlug.TryGetValue(e.Slug, out var n) && n.RequiresChildSelection)
                    q.MissingChild.Add(row);
            }
        }
        q.UnclassifiedEquity = q.UnclassifiedEquity.OrderBy(r => r.Code).ToList();
        q.Conflicting = q.Conflicting.OrderBy(r => r.Code).ToList();
        q.MissingChild = q.MissingChild.OrderBy(r => r.Code).ToList();
        return q;
    }

    public async Task<ReclassifyResult> ReclassifyUnclassifiedAsync(string performedBy, CancellationToken ct = default)
    {
        var result = new ReclassifyResult();
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var fundRepo = sp.GetRequiredService<IFundRepository>();
        var nodeRepo = sp.GetRequiredService<IFundClassificationNodeRepository>();
        var overrideRepo = sp.GetRequiredService<IFundClassificationOverrideRepository>();
        var logRepo = sp.GetRequiredService<IFundClassificationChangeLogRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var ctx = await LoadContextAsync(ct);
        if (!ctx.BySlug.TryGetValue(UnclassifiedSlug, out var unclassified))
            return result;

        var funds = (await fundRepo.GetAllAsync(ct)).Where(f => f.IsActive).ToList();
        var eff = await ResolveEffectiveManyAsync(funds, ctx);
        var eligible = eff.Where(kv => string.Equals(kv.Value.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(kv.Value.Source, "Manual", StringComparison.OrdinalIgnoreCase)).ToList();
        var byId = funds.ToDictionary(f => f.Id);

        foreach (var (fundId, _) in eligible)
        {
            result.Processed++;
            try
            {
                if (!byId.TryGetValue(fundId, out var fund)) { result.Failed++; continue; }
                var fresh = ResolveEffective(fund, ctx);
                if (!string.Equals(fresh.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase) && fresh.NodeId.HasValue)
                {
                    // Replace stale AutoSync rows with the new automatic assignment.
                    var stale = (await overrideRepo.GetByFundIdAsync(fundId, ct))
                        .Where(o => string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var s in stale) overrideRepo.Delete(s);
                    await overrideRepo.AddAsync(new FundClassificationOverride
                    {
                        FundId = fundId,
                        NodeId = fresh.NodeId.Value,
                        Action = ClassificationOverrideAction.Include,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "AutoSync"
                    }, ct);
                    await logRepo.AddAsync(new FundClassificationChangeLog
                    {
                        FundId = fundId,
                        NodeId = fresh.NodeId,
                        Action = ClassificationChangeAction.Reclassified,
                        PerformedBy = performedBy,
                        Reason = fresh.Reason,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                    result.Moved++;
                    result.MovedCodes.Add(fund.Code);
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"{fundId}: {ex.Message}");
            }
        }

        await uow.SaveChangesAsync(ct);
        result.Remaining = eligible.Count - result.Moved;
        _logger.LogInformation("Reclassified unclassified funds: processed {P}, moved {M}, remaining {R}",
            result.Processed, result.Moved, result.Remaining);
        return result;
    }

    public async Task<List<FundClassificationNode>> GetPublicTreeAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        return (await repo.GetAllAsync(false, ct)).Where(n => n.IsVisible && !n.IsDeleted).ToList();
    }

    /// <summary>
    /// Sync-time recalculation for one fund. Recomputes the automatic result and
    /// upserts the stored AutoSync row; manual assignments are never touched.
    /// Safe to call for funds whose tree rows do not exist yet (no-op).
    /// </summary>
    public async Task SyncFundClassificationAsync(int fundId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var fundRepo = sp.GetRequiredService<IFundRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var db = sp.GetRequiredService<Meezan.Infrastructure.Persistence.ApplicationDbContext>();

        var fund = await fundRepo.GetByIdAsync(fundId, ct);
        if (fund == null || !fund.IsActive) return;

        var ctx = await LoadContextAsync(ct);
        if (!ctx.BySlug.Any()) return; // tree not seeded yet

        // Manual assignment present → drop stale AutoSync rows, keep manual.
        bool hasManual = ctx.OverridesByFund.TryGetValue(fundId, out var rows)
            && rows.Any(o => o.Action == ClassificationOverrideAction.Include && !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase));
        var autoRows = await db.FundClassificationOverrides
            .Where(o => o.FundId == fundId && o.CreatedBy == "AutoSync").ToListAsync(ct);

        if (hasManual)
        {
            if (autoRows.Count > 0)
            {
                db.FundClassificationOverrides.RemoveRange(autoRows);
                await uow.SaveChangesAsync(ct);
            }
            return;
        }

        var eff = ResolveEffective(fund, ctx);
        int? wantNode = string.Equals(eff.Source, "Auto", StringComparison.OrdinalIgnoreCase) ? eff.NodeId : null;

        var currentAutoId = autoRows.Where(o => o.Action == ClassificationOverrideAction.Include)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Select(o => (int?)o.NodeId).FirstOrDefault();

        if (wantNode == currentAutoId) return; // already correct (covers both null)

        if (autoRows.Count > 0) db.FundClassificationOverrides.RemoveRange(autoRows);
        if (wantNode.HasValue)
        {
            await db.FundClassificationOverrides.AddAsync(new FundClassificationOverride
            {
                FundId = fundId,
                NodeId = wantNode.Value,
                Action = ClassificationOverrideAction.Include,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "AutoSync"
            }, ct);
        }
        await uow.SaveChangesAsync(ct);
    }

    // ---------- seed + migrate ----------

    public async Task<ClassificationMigrationReport> EnsureSeedAndMigrateAsync(string performedBy, CancellationToken ct = default)
    {
        var report = new ClassificationMigrationReport();
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;
        var nodeRepo = sp.GetRequiredService<IFundClassificationNodeRepository>();
        var fundRepo = sp.GetRequiredService<IFundRepository>();
        var catRepo = sp.GetRequiredService<IFundCategoryRepository>();
        var catOverrideRepo = sp.GetRequiredService<IFundCategoryOverrideRepository>();
        var overrideRepo = sp.GetRequiredService<IFundClassificationOverrideRepository>();
        var logRepo = sp.GetRequiredService<IFundClassificationChangeLogRepository>();
        var backupRepo = sp.GetRequiredService<IFundClassificationBackupRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var db = sp.GetRequiredService<Meezan.Infrastructure.Persistence.ApplicationDbContext>();

        // 0. Backup (idempotent).
        if (!await backupRepo.HasAnyAsync(ct))
        {
            var funds = await fundRepo.GetAllAsync(ct);
            var cats = await catRepo.GetAllAsync(true, ct);
            var catById = cats.ToDictionary(c => c.Id);
            var allCatOverrides = await db.FundCategoryOverrides.AsNoTracking().ToListAsync(ct);
            var allTypeOverrides = await db.FundTypeOverrides.AsNoTracking().ToListAsync(ct);
            var typeDefs = await db.FundTypeDefinitions.AsNoTracking().ToListAsync(ct);
            var typeById = typeDefs.ToDictionary(t => t.Id);
            var rows = new List<FundClassificationBackup>();
            foreach (var f in funds.Where(f => f.IsActive))
            {
                var fCat = allCatOverrides.Where(o => o.FundId == f.Id).ToList();
                string CatSlugs(IEnumerable<FundCategoryOverride> os)
                    => string.Join(",", os.Where(o => o.Action == OverrideAction.Include && catById.TryGetValue(o.CategoryId, out var c) && !c.IsDeleted).Select(o => catById[o.CategoryId].Slug).Distinct().OrderBy(s => s));
                var manualInc = fCat.Where(o => o.Action == OverrideAction.Include && !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)).ToList();
                var manualExc = fCat.Where(o => o.Action == OverrideAction.Exclude && !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)).ToList();
                var tOverride = allTypeOverrides.FirstOrDefault(o => o.FundId == f.Id);
                rows.Add(new FundClassificationBackup
                {
                    FundId = f.Id,
                    FundCode = f.Code,
                    FundNameAr = f.NameAr,
                    FundNameEn = f.NameEn,
                    OldType = (int)f.Type,
                    OldTypeSlug = FundTypeCatalog.SlugFor(f.Type),
                    OldTypeOverrideSlug = tOverride != null && typeById.TryGetValue(tOverride.FundTypeDefinitionId, out var td) ? td.Slug : null,
                    OldCategorySlugs = CatSlugs(fCat),
                    OldManualIncludeSlugs = string.Join(",", manualInc.Where(o => catById.ContainsKey(o.CategoryId)).Select(o => catById[o.CategoryId].Slug).Distinct().OrderBy(s => s)),
                    OldManualExcludeSlugs = string.Join(",", manualExc.Where(o => catById.ContainsKey(o.CategoryId)).Select(o => catById[o.CategoryId].Slug).Distinct().OrderBy(s => s)),
                    BackedUpAt = DateTime.UtcNow,
                    BackedUpBy = performedBy
                });
            }
            await backupRepo.AddRangeAsync(rows, ct);
            await uow.SaveChangesAsync(ct);
            report.BackupPerformed = true;
            report.BackupRows = rows.Count;
            _logger.LogInformation("Classification backup captured for {Count} funds", rows.Count);
        }

        // 1. Rename commodities → silver in FundTypeDefinitions (+ overrides + changelog refs).
        var typeDefsAll = await db.FundTypeDefinitions.IgnoreQueryFilters().ToListAsync(ct);
        var comm = typeDefsAll.FirstOrDefault(t => string.Equals(t.Slug, CommoditiesSlug, StringComparison.OrdinalIgnoreCase));
        var silverDef = typeDefsAll.FirstOrDefault(t => string.Equals(t.Slug, SilverSlug, StringComparison.OrdinalIgnoreCase));
        if (comm != null && silverDef == null)
        {
            comm.Slug = SilverSlug;
            if (comm.NameAr == "صندوق سلع" || comm.NameAr == "صندوق فضه") comm.NameAr = "صندوق فضة";
            if (comm.NameEn == "Commodities Fund" || comm.NameEn == "Silver Fund") comm.NameEn = "Silver Fund";
            comm.UpdatedAt = DateTime.UtcNow;
            comm.UpdatedBy = performedBy;
            await uow.SaveChangesAsync(ct);
            report.Notes.Add("FundTypeDefinitions: slug 'commodities' renamed to 'silver'.");
        }

        // 2. Seed tree (preserve edited names/order/visibility; never recreate deleted).
        var existing = await nodeRepo.GetAllAsync(true, ct);
        var bySlug = existing.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);
        bool seedChanged = false;

        // Handle legacy 'commodities' tree node if present (from earlier partial work).
        if (bySlug.TryGetValue(CommoditiesSlug, out var legacyComm) && !bySlug.ContainsKey(SilverSlug))
        {
            legacyComm.Slug = SilverSlug;
            if (legacyComm.NameAr == "صندوق سلع" || legacyComm.NameAr == "صندوق فضه") legacyComm.NameAr = "صندوق فضة";
            if (legacyComm.NameEn == "Commodities Fund" || legacyComm.NameEn == "Silver Fund") legacyComm.NameEn = "Silver Fund";
            legacyComm.UpdatedAt = DateTime.UtcNow;
            legacyComm.UpdatedBy = performedBy;
            nodeRepo.Update(legacyComm);
            seedChanged = true;
            report.Notes.Add("Tree: legacy 'commodities' node renamed to 'silver'.");
        }

        foreach (var seed in SeedTree)
        {
            if (bySlug.TryGetValue(seed.Slug, out var cur))
            {
                bool touched = false;
                // Backfill structural/optional fields only when empty; never overwrite names/order/visibility.
                if (cur.Level != seed.Level)
                {
                    // Recompute level from parent chain instead of trusting stale data.
                    cur.Level = seed.Level;
                    touched = true;
                }
                if (string.IsNullOrWhiteSpace(cur.ChildrenLabelAr)) { cur.ChildrenLabelAr = seed.ChildrenLabelAr; touched = true; }
                if (string.IsNullOrWhiteSpace(cur.ChildrenLabelEn)) { cur.ChildrenLabelEn = seed.ChildrenLabelEn; touched = true; }
                if (string.IsNullOrWhiteSpace(cur.AutoRuleJson) && !string.IsNullOrWhiteSpace(seed.AutoRuleJson)) { cur.AutoRuleJson = seed.AutoRuleJson; touched = true; }
                if (touched) { cur.UpdatedAt = DateTime.UtcNow; cur.UpdatedBy = performedBy; nodeRepo.Update(cur); seedChanged = true; }
                continue;
            }
            // Never recreate deleted nodes: 'unknown' stays deleted.
            if (string.Equals(seed.Slug, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                if (!bySlug.ContainsKey("unknown"))
                {
                    var now = DateTime.UtcNow;
                    var deleted = new FundClassificationNode
                    {
                        Slug = "unknown",
                        NameAr = "غير محدد",
                        NameEn = "Unclassified",
                        Level = seed.Level,
                        ParentId = null,
                        Icon = string.Empty,
                        Color = string.Empty,
                        DisplayOrder = 99,
                        IsVisible = false,
                        IsDeleted = true,
                        DeletedAt = now,
                        DeletedReason = "Legacy top-level node retired by classification-tree migration.",
                        RequiresChildSelection = false,
                        ChildrenLabelAr = "التصنيف الفرعي",
                        ChildrenLabelEn = "Sub classification",
                        AutoRuleJson = string.Empty,
                        AutoClassificationEnabled = false,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedBy = performedBy,
                        UpdatedBy = performedBy
                    };
                    await nodeRepo.AddAsync(deleted, ct);
                    bySlug["unknown"] = deleted;
                    seedChanged = true;
                }
                continue;
            }
            int? parentId = null;
            if (seed.ParentSlug != null)
            {
                if (!bySlug.TryGetValue(seed.ParentSlug, out var parent))
                    continue; // parent not seeded yet; handled by level-ordered loop below
                // Parent must already be persisted (real Id) — the seed loop below
                // inserts level by level and saves between levels.
                if (parent.Id <= 0) continue;
                parentId = parent.Id;
            }
            var now2 = DateTime.UtcNow;
            var node = new FundClassificationNode
            {
                Slug = seed.Slug,
                NameAr = seed.NameAr,
                NameEn = seed.NameEn,
                Level = seed.Level,
                ParentId = parentId,
                Icon = string.Empty,
                Color = string.Empty,
                DisplayOrder = seed.DisplayOrder,
                IsVisible = seed.IsVisible,
                IsDeleted = false,
                RequiresChildSelection = seed.RequiresChildSelection,
                ChildrenLabelAr = seed.ChildrenLabelAr,
                ChildrenLabelEn = seed.ChildrenLabelEn,
                AutoRuleJson = seed.AutoRuleJson,
                AutoClassificationEnabled = true,
                CreatedAt = now2,
                UpdatedAt = now2,
                CreatedBy = performedBy,
                UpdatedBy = performedBy
            };
            await nodeRepo.AddAsync(node, ct);
            bySlug[seed.Slug] = node;
            seedChanged = true;
        }

        if (seedChanged)
        {
            await uow.SaveChangesAsync(ct);
            // Refresh ids/parents (children added before parents flushed need ParentId fix-up).
            existing = await nodeRepo.GetAllAsync(true, ct);
            bySlug = existing.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);
            foreach (var seed in SeedTree.Where(s => s.ParentSlug != null))
            {
                if (bySlug.TryGetValue(seed.Slug, out var n) && bySlug.TryGetValue(seed.ParentSlug!, out var p) && n.ParentId != p.Id)
                {
                    n.ParentId = p.Id;
                    n.Level = seed.Level;
                    n.UpdatedAt = DateTime.UtcNow;
                    n.UpdatedBy = performedBy;
                    nodeRepo.Update(n);
                }
            }
            await uow.SaveChangesAsync(ct);
            existing = await nodeRepo.GetAllAsync(true, ct);
            bySlug = existing.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);
        }

        // Retired 'unknown' tombstone: the legacy top-level node stays soft-deleted
        // (never recreated by sync/seed) so its slug remains blocked from reuse.
        if (!bySlug.ContainsKey("unknown"))
        {
            var now3 = DateTime.UtcNow;
            await nodeRepo.AddAsync(new FundClassificationNode
            {
                Slug = "unknown",
                NameAr = "غير محدد",
                NameEn = "Unclassified",
                Level = 0,
                ParentId = null,
                Icon = string.Empty,
                Color = string.Empty,
                DisplayOrder = 99,
                IsVisible = false,
                IsDeleted = true,
                DeletedAt = now3,
                DeletedReason = "Legacy top-level node retired by classification-tree migration.",
                RequiresChildSelection = false,
                ChildrenLabelAr = "التصنيف الفرعي",
                ChildrenLabelEn = "Sub classification",
                AutoRuleJson = string.Empty,
                AutoClassificationEnabled = false,
                CreatedAt = now3,
                UpdatedAt = now3,
                CreatedBy = performedBy,
                UpdatedBy = performedBy
            }, ct);
            await uow.SaveChangesAsync(ct);
            existing = await nodeRepo.GetAllAsync(true, ct);
            bySlug = existing.ToDictionary(n => n.Slug, StringComparer.OrdinalIgnoreCase);
        }

        report.SeedPerformed = seedChanged;

        // 3. Migrate assignments.
        var funds2 = (await fundRepo.GetAllAsync(ct)).Where(f => f.IsActive).ToList();
        var migCatOverrides = await db.FundCategoryOverrides.AsNoTracking().ToListAsync(ct);
        var migTypeOverrides = await db.FundTypeOverrides.AsNoTracking().ToListAsync(ct);
        var typeDefs2 = await db.FundTypeDefinitions.IgnoreQueryFilters().ToListAsync(ct);
        var typeById2 = typeDefs2.ToDictionary(t => t.Id);
        var cats2 = await catRepo.GetAllAsync(true, ct);
        var catById2 = cats2.ToDictionary(c => c.Id);
        var catBySlug = cats2.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        var ctx = await LoadContextAsync(ct);
        var changedCount = 0;
        foreach (var fund in funds2)
        {
            // Old effective path (type slug + manual/auto category slugs).
            var oldTypeSlug = FundTypeCatalog.SlugFor(fund.Type);
            if (string.Equals(oldTypeSlug, CommoditiesSlug, StringComparison.OrdinalIgnoreCase)) oldTypeSlug = SilverSlug;
            var tOv = migTypeOverrides.FirstOrDefault(o => o.FundId == fund.Id);
            string? oldTypeOverride = tOv != null && typeById2.TryGetValue(tOv.FundTypeDefinitionId, out var otd)
                ? (string.Equals(otd.Slug, CommoditiesSlug, StringComparison.OrdinalIgnoreCase) ? SilverSlug : otd.Slug) : null;
            var fCat = migCatOverrides.Where(o => o.FundId == fund.Id && o.Action == OverrideAction.Include).ToList();
            var oldCats = string.Join(",", fCat.Where(o => catBySlug.ContainsKey(cats2.First(c => c.Id == o.CategoryId).Slug)).Select(o => cats2.First(c => c.Id == o.CategoryId).Slug).Distinct().OrderBy(s => s));

            // Determine new node:
            // a) manual type override → mapped top-level node.
            // b) manual index-category includes → corresponding index tree nodes (first valid).
            // c) manual active include → equity > active.
            // d) otherwise auto/SNDUK via resolver.
            int? targetNodeId = null;
            string reason;
            string source;
            if (tOv != null && typeById2.TryGetValue(tOv.FundTypeDefinitionId, out var mtd))
            {
                var slug = string.Equals(mtd.Slug, CommoditiesSlug, StringComparison.OrdinalIgnoreCase) ? SilverSlug : mtd.Slug;
                if (ctx.BySlug.TryGetValue(slug, out var tn) && !tn.IsDeleted)
                {
                    targetNodeId = tn.Id;
                    source = "Manual";
                    reason = $"نوع يدوي سابق: {slug}";
                }
                else { source = "Manual"; reason = $"نوع يدوي سابق '{slug}' غير صالح — أعيد للتلقائي"; targetNodeId = null; }
            }
            else
            {
                // Manual index includes?
                var manualIdx = fCat.Where(o => !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)
                    && catById2.TryGetValue(o.CategoryId, out var cc) && IsIndexCategorySlug(cc.Slug)).ToList();
                if (manualIdx.Count > 0)
                {
                    var cc = catBySlug[catBySlug.First(kv => manualIdx.Any(o => cats2.First(c => c.Id == o.CategoryId).Slug == kv.Key)).Key];
                    if (ctx.BySlug.TryGetValue(cc.Slug, out var inode) && !inode.IsDeleted)
                    {
                        targetNodeId = inode.Id;
                        source = "Manual";
                        reason = $"تصنيف مؤشر يدوي سابق: {cc.Slug}";
                    }
                    else { source = "Auto"; reason = "أعيد للتلقائي"; }
                }
                else
                {
                    var manualActive = fCat.Any(o => !string.Equals(o.CreatedBy, "AutoSync", StringComparison.OrdinalIgnoreCase)
                        && catById2.TryGetValue(o.CategoryId, out var ac) && string.Equals(ac.Slug, "actively-managed", StringComparison.OrdinalIgnoreCase));
                    if (manualActive && ctx.BySlug.TryGetValue("active", out var anode) && !anode.IsDeleted)
                    {
                        targetNodeId = anode.Id;
                        source = "Manual";
                        reason = "تضمين يدوي سابق في actively-managed";
                    }
                    else
                    {
                        var eff = ResolveEffective(fund, ctx);
                        if (eff.NodeId.HasValue) { targetNodeId = eff.NodeId; source = eff.Source; reason = eff.Reason; }
                        else { source = "None"; reason = eff.Reason; }
                    }
                }
            }

            // Persist manual assignments only (auto results are recomputed, not stored).
            if (string.Equals(source, "Manual", StringComparison.OrdinalIgnoreCase) && targetNodeId.HasValue)
            {
                var exist = await db.FundClassificationOverrides.FirstOrDefaultAsync(o => o.FundId == fund.Id && o.NodeId == targetNodeId.Value, ct);
                if (exist == null)
                {
                    await db.FundClassificationOverrides.AddAsync(new FundClassificationOverride
                    {
                        FundId = fund.Id,
                        NodeId = targetNodeId.Value,
                        Action = ClassificationOverrideAction.Include,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = performedBy
                    }, ct);
                    await db.FundClassificationChangeLogs.AddAsync(new FundClassificationChangeLog
                    {
                        FundId = fund.Id,
                        NodeId = targetNodeId.Value,
                        Action = ClassificationChangeAction.ManualIncluded,
                        PerformedBy = performedBy,
                        Reason = "ترحيل: " + reason,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }
            }

            // Report row when effective path changed.
            var newEff = await ResolveEffectiveWithOverrideAsync(fund, targetNodeId, source, ctx);
            var oldPath = oldTypeOverride ?? oldTypeSlug;
            if (!string.IsNullOrEmpty(oldCats)) oldPath += " + [" + oldCats + "]";
            if (!string.Equals(oldPath.Trim(), newEff.DisplayPathAr.Trim(), StringComparison.Ordinal)
                || !string.Equals((oldTypeOverride ?? oldTypeSlug), newEff.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var brow = await db.FundClassificationBackups.FirstOrDefaultAsync(b => b.FundId == fund.Id, ct);
                if (brow != null)
                {
                    brow.NewPath = newEff.DisplayPathAr;
                    brow.MigrationReason = $"{source}: {reason}";
                }
                report.Changed.Add(new ChangedFund
                {
                    Code = fund.Code,
                    Name = fund.NameAr,
                    OldPath = oldPath,
                    NewPath = newEff.DisplayPathAr,
                    Reason = $"{source}: {reason}"
                });
                changedCount++;
                if (string.Equals(newEff.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
                    report.UnclassifiedEquity.Add($"{fund.Code} — {fund.NameAr}");
            }
        }

        await uow.SaveChangesAsync(ct);

        // 4. Commodities → silver counts.
        var commFunds = funds2.Where(f => f.Type == FundType.Commodities).ToList();
        report.CommoditiesRenamed = commFunds.Count;
        report.CommoditiesFunds = commFunds.Select(f => $"{f.Code} — {f.NameAr}").OrderBy(s => s).ToList();

        // 5. Retire old index/active categories (keep Sharia + custom). Only after migration.
        string[] retiredSlugs = ["index-egx30", "index-egx33", "index-egx70", "index-egx100", "tracks-egx35", "actively-managed"];
        foreach (var slug in retiredSlugs)
        {
            if (catBySlug.TryGetValue(slug, out var c) && !c.IsDeleted)
            {
                c.IsDeleted = true;
                c.DeletedAt = DateTime.UtcNow;
                c.DeletedReason = "Retired by classification-tree migration; assignments migrated to tree nodes.";
                c.UpdatedAt = DateTime.UtcNow;
                c.UpdatedBy = performedBy;
                catRepo.Update(c);
                report.Notes.Add($"Retired old category '{slug}'.");
            }
        }
        await uow.SaveChangesAsync(ct);

        _logger.LogInformation("Classification seed+migrate done: backup={Backup}, changed={Changed}, silver={Silver}",
            report.BackupRows, changedCount, report.CommoditiesRenamed);
        return report;
    }

    private static bool IsIndexCategorySlug(string slug)
        => string.Equals(slug, "index-egx30", StringComparison.OrdinalIgnoreCase)
        || string.Equals(slug, "index-egx33", StringComparison.OrdinalIgnoreCase)
        || string.Equals(slug, "index-egx70", StringComparison.OrdinalIgnoreCase)
        || string.Equals(slug, "index-egx100", StringComparison.OrdinalIgnoreCase)
        || string.Equals(slug, "tracks-egx35", StringComparison.OrdinalIgnoreCase);

    private Task<EffectiveClassification> ResolveEffectiveWithOverrideAsync(Fund fund, int? manualNodeId, string source, ClassificationContext ctx)
    {
        if (string.Equals(source, "Manual", StringComparison.OrdinalIgnoreCase) && manualNodeId.HasValue
            && ctx.ById.TryGetValue(manualNodeId.Value, out var n) && !n.IsDeleted)
        {
            var path = BuildPath(n, ctx.ById);
            return Task.FromResult(new EffectiveClassification
            {
                NodeId = n.Id,
                Slug = n.Slug,
                PathSlugs = path.Select(x => x.Slug).ToList(),
                PathNamesAr = path.Select(x => x.NameAr).ToList(),
                PathNamesEn = path.Select(x => x.NameEn).ToList(),
                Source = "Manual",
                Reason = "تعيين يدوي مرحّل",
                NeedsReview = n.RequiresChildSelection,
                ReviewReasons = n.RequiresChildSelection ? new List<string> { "العقدة تتطلب اختيار تصنيف فرعي" } : new List<string>()
            });
        }
        return ResolveEffectiveAsync(fund, ctx);
    }
}
