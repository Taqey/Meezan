using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Funds;

public class FundRepository : IFundRepository
{
    private readonly ApplicationDbContext _context;

    public FundRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Fund?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Read-only lookup for the detail endpoint. The child collections
        // (price history, analysis, similars, overrides) are each loaded by their
        // own purpose-built repository, so loading them here as well would
        // materialise a cartesian product AND duplicate every row.
        return await _context.Funds
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<Fund?> GetBySndukIdAsync(long sndukId, CancellationToken cancellationToken = default)
    {
        return await _context.Funds
            .FirstOrDefaultAsync(f => f.SndukId == sndukId, cancellationToken);
    }

    public async Task<Fund?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Funds
            .FirstOrDefaultAsync(f => f.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public async Task<Fund?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Funds
            .FirstOrDefaultAsync(f => f.Slug == slug, cancellationToken);
    }

    public async Task<List<Fund>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Funds
            .OrderBy(f => f.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Fund>> GetAllStaticAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Funds
            .AsNoTracking()
            .Select(f => new Fund
            {
                Id = f.Id,
                SndukId = f.SndukId,
                PublicId = f.PublicId,
                Slug = f.Slug,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                Code = f.Code,
                Type = f.Type,
                TypeNameAr = f.TypeNameAr,
                TypeNameEn = f.TypeNameEn,
                RiskLevel = f.RiskLevel,
                IsIslamic = f.IsIslamic,
                IsFeatured = f.IsFeatured,
                IsActive = f.IsActive,
                TotalScore = f.TotalScore,
                Aum = f.Aum,
                ManagementFee = f.ManagementFee,
                MinimumInvestment = f.MinimumInvestment,
                Currency = f.Currency,
                AssetManagerId = f.AssetManagerId,
                AssetManagerName = f.AssetManagerName,
                AssetManagerNameEn = f.AssetManagerNameEn,
                AssetManagerCode = f.AssetManagerCode,
                LogoUrl = f.LogoUrl,
                LocalLogoPath = f.LocalLogoPath,
                DescriptionAr = f.DescriptionAr,
                DescriptionEn = f.DescriptionEn,
                InvestmentStrategy = f.InvestmentStrategy,
                InceptionDate = f.InceptionDate,
                ProspectusUrl = f.ProspectusUrl,
                LastSyncedAt = f.LastSyncedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Fund> Items, int TotalCount)> QueryPagedAsync(FundListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Funds
            .Where(f => f.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(f =>
                f.NameAr.Contains(s) ||
                f.NameEn.Contains(s) ||
                f.Code.Contains(s));
        }

        FundType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            parsedType = FundTypeCatalog.EnumFor(filter.Type);
            query = query.Where(f => f.Type == parsedType);
        }

        FundRiskLevel? parsedRiskLevel = null;
        if (!string.IsNullOrWhiteSpace(filter.RiskLevel))
        {
            // Unknown risk values return an empty 200 result, never a 500.
            if (!Enum.TryParse<FundRiskLevel>(filter.RiskLevel.Trim(), true, out var parsed) || !Enum.IsDefined(parsed))
                return (new List<Fund>(), 0);
            parsedRiskLevel = parsed;
            query = query.Where(f => f.RiskLevel == parsedRiskLevel);
        }

        if (filter.AssetManagerId.HasValue)
            query = query.Where(f => f.AssetManagerId == filter.AssetManagerId.Value);

        if (filter.IsIslamic.HasValue)
            query = query.Where(f => f.IsIslamic == filter.IsIslamic.Value);

        if (filter.IsFeatured.HasValue)
            query = query.Where(f => f.IsFeatured == filter.IsFeatured.Value);

        if (filter.CategoryId.HasValue)
        {
            // Join with category overrides to get funds in category
            var includeIds = await _context.FundCategoryOverrides
                .Where(o => o.CategoryId == filter.CategoryId.Value && o.Action == OverrideAction.Include)
                .Select(o => o.FundId)
                .ToListAsync(cancellationToken);

            var excludeIds = await _context.FundCategoryOverrides
                .Where(o => o.CategoryId == filter.CategoryId.Value && o.Action == OverrideAction.Exclude)
                .Select(o => o.FundId)
                .ToListAsync(cancellationToken);

            // Also get auto-derived categories
            var autoCategoryIds = await _context.FundCategories
                .Where(c => c.Id == filter.CategoryId.Value && c.IsAutoDerived && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            // This is simplified - in reality we'd need to evaluate the auto rules
            // For now, just use manual overrides
            query = query.Where(f => includeIds.Contains(f.Id) && !excludeIds.Contains(f.Id));
        }

        if (filter.MinManagementFee.HasValue)
            query = query.Where(f => f.ManagementFee >= filter.MinManagementFee.Value);

        if (filter.MaxManagementFee.HasValue)
            query = query.Where(f => f.ManagementFee <= filter.MaxManagementFee.Value);

        if (filter.MinInvestment.HasValue)
            query = query.Where(f => f.Aum >= filter.MinInvestment.Value);

        if (filter.MaxInvestment.HasValue)
            query = query.Where(f => f.Aum <= filter.MaxInvestment.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var sortBy = filter.SortBy ?? "return1y";
        var descending = filter.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

        query = (sortBy, descending) switch
        {
            ("return1m", true) => query.OrderByDescending(f => f.Return1M),
            ("return1m", false) => query.OrderBy(f => f.Return1M),
            ("return3m", true) => query.OrderByDescending(f => f.Return3M),
            ("return3m", false) => query.OrderBy(f => f.Return3M),
            ("return6m", true) => query.OrderByDescending(f => f.Return6M),
            ("return6m", false) => query.OrderBy(f => f.Return6M),
            ("return1y", true) => query.OrderByDescending(f => f.Return1Y),
            ("return1y", false) => query.OrderBy(f => f.Return1Y),
            ("return3y", true) => query.OrderByDescending(f => f.Return3Y),
            ("return3y", false) => query.OrderBy(f => f.Return3Y),
            ("aum", true) => query.OrderByDescending(f => f.Aum),
            ("aum", false) => query.OrderBy(f => f.Aum),
            ("managementfee", true) => query.OrderByDescending(f => f.ManagementFee),
            ("managementfee", false) => query.OrderBy(f => f.ManagementFee),
            ("totalscore", true) => query.OrderByDescending(f => f.TotalScore),
            ("totalscore", false) => query.OrderBy(f => f.TotalScore),
            ("name", true) => query.OrderByDescending(f => f.NameAr),
            ("name", false) => query.OrderBy(f => f.NameAr),
            ("currentprice", true) => query.OrderByDescending(f => f.CurrentPrice),
            ("currentprice", false) => query.OrderBy(f => f.CurrentPrice),
            _ => query.OrderByDescending(f => f.Return1Y),
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Fund fund, CancellationToken cancellationToken = default)
    {
        await _context.Funds.AddAsync(fund, cancellationToken);
    }

    public void Update(Fund fund)
    {
        _context.Funds.Update(fund);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

public class FundPriceHistoryRepository : IFundPriceHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public FundPriceHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundPriceHistory>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundPriceHistory>> GetByFundIdAndDateRangeAsync(int fundId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId && p.Date >= from && p.Date <= to)
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundPriceHistory?> GetLatestAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .OrderByDescending(p => p.Date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FundPriceHistory?> GetByFundIdAndDateAsync(int fundId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .FirstOrDefaultAsync(p => p.FundId == fundId && p.Date.Date == date.Date, cancellationToken);
    }

    public async Task<DateTime?> GetMaxDateAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundPriceHistories
            .Where(p => p.FundId == fundId)
            .MaxAsync(p => (DateTime?)p.Date, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundPriceHistory> history, CancellationToken cancellationToken = default)
    {
        await _context.FundPriceHistories.AddRangeAsync(history, cancellationToken);
    }

    public void UpdateRange(IEnumerable<FundPriceHistory> history)
    {
        _context.FundPriceHistories.UpdateRange(history);
    }
}

public class FundAnalysisRepository : IFundAnalysisRepository
{
    private readonly ApplicationDbContext _context;

    public FundAnalysisRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundAnalysis?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        // Check the change tracker first: rows Added earlier in the same unit of
        // work are not in the database yet, and adding a second one for the same
        // fund would violate IX_FundAnalyses_FundId on save.
        return _context.FundAnalyses.Local.FirstOrDefault(a => a.FundId == fundId)
            ?? await _context.FundAnalyses
                .FirstOrDefaultAsync(a => a.FundId == fundId, cancellationToken);
    }

    public async Task<List<FundAnalysis>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundAnalyses
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundAnalysis analysis, CancellationToken cancellationToken = default)
    {
        await _context.FundAnalyses.AddAsync(analysis, cancellationToken);
    }

    public void Update(FundAnalysis analysis)
    {
        _context.FundAnalyses.Update(analysis);
    }
}

public class FundSimilarRepository : IFundSimilarRepository
{
    private readonly ApplicationDbContext _context;

    public FundSimilarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundSimilar>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        // SimilarFund must be loaded: the detail page renders each similar fund's logo.
        return await _context.FundSimilars
            .Include(s => s.SimilarFund)
            .Where(s => s.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(int FundId, int SimilarFundId)>> GetAllSimilarLinksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSimilars
            .AsNoTracking()
            .Select(s => new ValueTuple<int, int>(s.FundId, s.SimilarFundId))
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundSimilar> similars, CancellationToken cancellationToken = default)
    {
        await _context.FundSimilars.AddRangeAsync(similars, cancellationToken);
    }

    public void ClearByFundId(int fundId)
    {
        var existing = _context.FundSimilars.Where(s => s.FundId == fundId);
        _context.FundSimilars.RemoveRange(existing);
    }
}


public class FundCategoryRepository : IFundCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<FundCategory?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<List<FundCategory>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundCategories.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);
        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategory>> GetVisibleAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundCategories
            .Where(c => c.IsVisible && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.NameAr)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundCategory category, CancellationToken cancellationToken = default)
    {
        await _context.FundCategories.AddAsync(category, cancellationToken);
    }

    public void Update(FundCategory category)
    {
        _context.FundCategories.Update(category);
    }

    public void Delete(FundCategory category)
    {
        _context.FundCategories.Remove(category);
    }
}

public class FundCategoryOverrideRepository : IFundCategoryOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundCategoryOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .Where(o => o.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategoryOverride>> GetByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .Where(o => o.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundCategoryOverride>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<FundCategoryOverride?> GetAsync(int fundId, int categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.FundCategoryOverrides
            .FirstOrDefaultAsync(o => o.FundId == fundId && o.CategoryId == categoryId, cancellationToken);
    }

    public async Task AddAsync(FundCategoryOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundCategoryOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundCategoryOverride @override)
    {
        _context.FundCategoryOverrides.Update(@override);
    }

    public void Delete(FundCategoryOverride @override)
    {
        _context.FundCategoryOverrides.Remove(@override);
    }

    public async Task ClearByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        var existing = _context.FundCategoryOverrides.Where(o => o.FundId == fundId);
        _context.FundCategoryOverrides.RemoveRange(existing);
        await Task.CompletedTask;
    }

    public async Task ClearByCategoryIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var existing = _context.FundCategoryOverrides.Where(o => o.CategoryId == categoryId);
        _context.FundCategoryOverrides.RemoveRange(existing);
        await Task.CompletedTask;
    }
}

public class FundSyncLogRepository : IFundSyncLogRepository
{
    private readonly ApplicationDbContext _context;
    public FundSyncLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public FundSyncLog? LastAdded { get; private set; }

    public async Task<FundSyncLog?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncLogs
            .OrderByDescending(l => l.RunAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(List<FundSyncLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FundSyncLogs
            .OrderByDescending(l => l.RunAt)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(FundSyncLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundSyncLogs.AddAsync(log, cancellationToken);
        LastAdded = log;
    }

    public void Update(FundSyncLog log)
    {
        _context.FundSyncLogs.Update(log);
    }
}

public class FundSyncLogDetailRepository : IFundSyncLogDetailRepository
{
    private readonly ApplicationDbContext _context;

    public FundSyncLogDetailRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<FundSyncLogDetail> details, CancellationToken cancellationToken = default)
    {
        await _context.FundSyncLogDetails.AddRangeAsync(details, cancellationToken);
    }

    public async Task<List<FundSyncLogDetail>> GetByLogIdAsync(int logId, CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncLogDetails
            .Where(d => d.FundSyncLogId == logId)
            .OrderBy(d => d.FundCode)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

public class FundSyncSettingsRepository : IFundSyncSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public FundSyncSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundSyncSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundSyncSettings
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Update(FundSyncSettings settings)
    {
        _context.FundSyncSettings.Update(settings);
    }
}

public class FundCategoryChangeLogRepository : IFundCategoryChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundCategoryChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundCategoryChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundCategoryChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundCategoryChangeLog> Items, int TotalCount)> QueryPagedAsync(FundCategoryChangeLogFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.FundCategoryChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Category)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        if (filter.FundId.HasValue)
            query = query.Where(l => l.FundId == filter.FundId.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(l => l.CategoryId == filter.CategoryId.Value);

        if (filter.Action.HasValue)
            query = query.Where(l => l.Action == filter.Action.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(l => l.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

public class FundClassificationNodeRepository : IFundClassificationNodeRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationNodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundClassificationNode?> GetByIdAsync(int id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<FundClassificationNode?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsQueryable();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.FirstOrDefaultAsync(n => n.Slug.ToLower() == slug.Trim().ToLower(), cancellationToken);
    }

    public async Task<List<FundClassificationNode>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsNoTracking();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        return await query.OrderBy(n => n.Level).ThenBy(n => n.DisplayOrder).ThenBy(n => n.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<List<FundClassificationNode>> GetChildrenAsync(int parentId, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationNodes.AsNoTracking().Where(n => n.ParentId == parentId);
        if (includeDeleted) query = query.IgnoreQueryFilters();
        else query = query.Where(n => !n.IsDeleted);
        return await query.OrderBy(n => n.DisplayOrder).ThenBy(n => n.NameAr).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FundClassificationNode node, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationNodes.AddAsync(node, cancellationToken);
    }

    public void Update(FundClassificationNode node)
    {
        _context.FundClassificationNodes.Update(node);
    }

    public void Delete(FundClassificationNode node)
    {
        _context.FundClassificationNodes.Remove(node);
    }
}

public class FundClassificationOverrideRepository : IFundClassificationOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundClassificationOverride>> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .Where(o => o.FundId == fundId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<FundClassificationOverride>> GetByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .Where(o => o.NodeId == nodeId)
            .ToListAsync(cancellationToken);
    }

    public async Task<FundClassificationOverride?> GetAsync(int fundId, int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .FirstOrDefaultAsync(o => o.FundId == fundId && o.NodeId == nodeId, cancellationToken);
    }

    public async Task<int> CountByNodeIdAsync(int nodeId, CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationOverrides
            .AsNoTracking()
            .CountAsync(o => o.NodeId == nodeId, cancellationToken);
    }

    public async Task AddAsync(FundClassificationOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundClassificationOverride @override)
    {
        _context.FundClassificationOverrides.Update(@override);
    }

    public void Delete(FundClassificationOverride @override)
    {
        _context.FundClassificationOverrides.Remove(@override);
    }

    public void DeleteRange(IEnumerable<FundClassificationOverride> overrides)
    {
        _context.FundClassificationOverrides.RemoveRange(overrides);
    }
}

public class FundClassificationChangeLogRepository : IFundClassificationChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundClassificationChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundClassificationChangeLog> Items, int TotalCount)> QueryPagedAsync(int? fundId, int? nodeId, ClassificationChangeAction? action, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FundClassificationChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Node)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        if (fundId.HasValue)
            query = query.Where(l => l.FundId == fundId.Value);

        if (nodeId.HasValue)
            query = query.Where(l => l.NodeId == nodeId.Value);

        if (action.HasValue)
            query = query.Where(l => l.Action == action.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}

public class FundClassificationBackupRepository : IFundClassificationBackupRepository
{
    private readonly ApplicationDbContext _context;

    public FundClassificationBackupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationBackups.AnyAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FundClassificationBackup> rows, CancellationToken cancellationToken = default)
    {
        await _context.FundClassificationBackups.AddRangeAsync(rows, cancellationToken);
    }

    public async Task<List<FundClassificationBackup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundClassificationBackups.AsNoTracking().OrderBy(b => b.FundCode).ToListAsync(cancellationToken);
    }
}

public class FundTypeDefinitionRepository : IFundTypeDefinitionRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeDefinitionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FundTypeDefinition>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var q = _context.FundTypeDefinitions.AsNoTracking();
        if (!includeDeleted) q = q.Where(t => !t.IsDeleted);
        return await q.OrderBy(t => t.DisplayOrder).ThenBy(t => t.NameAr).ToListAsync(cancellationToken);
    }

    public async Task<FundTypeDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<FundTypeDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.FirstOrDefaultAsync(x => x.Slug.ToLower() == slug.Trim().ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeDefinitions.AnyAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(FundTypeDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeDefinitions.AddAsync(definition, cancellationToken);
    }

    public void Update(FundTypeDefinition definition)
    {
        _context.FundTypeDefinitions.Update(definition);
    }

    public void Delete(FundTypeDefinition definition)
    {
        _context.FundTypeDefinitions.Remove(definition);
    }

    public async Task<List<FundTypeDefinition>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.FundTypeDefinitions.Where(t => idList.Contains(t.Id)).ToListAsync(cancellationToken);
    }
}

public class FundTypeOverrideRepository : IFundTypeOverrideRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeOverrideRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FundTypeOverride?> GetByFundIdAsync(int fundId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.FirstOrDefaultAsync(o => o.FundId == fundId, cancellationToken);
    }

    public async Task<List<FundTypeOverride>> GetByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.Where(o => o.FundTypeDefinitionId == definitionId).ToListAsync(cancellationToken);
    }

    public async Task<List<FundTypeOverride>> GetByFundIdsAsync(IEnumerable<int> fundIds, CancellationToken cancellationToken = default)
    {
        var idList = fundIds.ToList();
        return await _context.FundTypeOverrides.Where(o => idList.Contains(o.FundId)).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetAllFundOverridesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.AsNoTracking().ToDictionaryAsync(o => o.FundId, o => o.FundTypeDefinitionId, cancellationToken);
    }

    public async Task<int> CountByDefinitionIdAsync(int definitionId, CancellationToken cancellationToken = default)
    {
        return await _context.FundTypeOverrides.CountAsync(o => o.FundTypeDefinitionId == definitionId, cancellationToken);
    }

    public async Task AddAsync(FundTypeOverride @override, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeOverrides.AddAsync(@override, cancellationToken);
    }

    public void Update(FundTypeOverride @override)
    {
        _context.FundTypeOverrides.Update(@override);
    }

    public void Delete(FundTypeOverride @override)
    {
        _context.FundTypeOverrides.Remove(@override);
    }

    public void DeleteRange(IEnumerable<FundTypeOverride> overrides)
    {
        _context.FundTypeOverrides.RemoveRange(overrides);
    }
}

public class FundTypeChangeLogRepository : IFundTypeChangeLogRepository
{
    private readonly ApplicationDbContext _context;

    public FundTypeChangeLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FundTypeChangeLog log, CancellationToken cancellationToken = default)
    {
        await _context.FundTypeChangeLogs.AddAsync(log, cancellationToken);
    }

    public async Task<(List<FundTypeChangeLog> Items, int TotalCount)> QueryPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var q = _context.FundTypeChangeLogs
            .Include(l => l.Fund)
            .Include(l => l.Definition)
            .OrderByDescending(l => l.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(cancellationToken);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
