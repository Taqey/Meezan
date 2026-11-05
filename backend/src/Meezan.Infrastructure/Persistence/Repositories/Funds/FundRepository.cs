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
                DisplayName = f.DisplayName,
                NameEn = f.NameEn,
                IsNameManuallyEdited = f.IsNameManuallyEdited,
                IsDescriptionManuallyEdited = f.IsDescriptionManuallyEdited,
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
                f.DisplayName.Contains(s) ||
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
