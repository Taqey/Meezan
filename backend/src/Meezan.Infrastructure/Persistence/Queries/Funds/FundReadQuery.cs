using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Queries.Funds;

public sealed class FundReadQuery : IFundReadQuery
{
    private readonly ApplicationDbContext _db;
    private readonly IFundRepository _fundRepo;
    private readonly IFundDetailQuery _detailQuery;

    public FundReadQuery(
        ApplicationDbContext db,
        IFundRepository fundRepo,
        IFundDetailQuery detailQuery)
    {
        _db = db;
        _fundRepo = fundRepo;
        _detailQuery = detailQuery;
    }

    public async Task<FundStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var q = _db.Funds.AsNoTracking().Where(f => f.IsActive);

        var totalCount = await q.CountAsync(ct);
        var islamicCount = await q.CountAsync(f => f.IsIslamic, ct);
        var totalAum = await q.SumAsync(f => (decimal?)(f.Aum ?? 0), ct) ?? 0m;

        var activeFunds = await q
            .Select(f => new { f.Id, f.Code, f.DailyChange })
            .ToListAsync(ct);

        // Map candidates from explicit DailyChange
        var candidates = activeFunds
            .Where(f => f.DailyChange.HasValue)
            .Select(f => new { Value = f.DailyChange!.Value, f.Code, f.Id })
            .ToList();

        // If any fund has null DailyChange, check if FundPriceHistories has history points
        var fundsNeedingHistory = activeFunds.Where(f => !f.DailyChange.HasValue).Select(f => f.Id).ToList();
        if (fundsNeedingHistory.Count > 0)
        {
            var histories = await _db.FundPriceHistories.AsNoTracking()
                .Where(h => fundsNeedingHistory.Contains(h.FundId))
                .OrderByDescending(h => h.Date)
                .ToListAsync(ct);

            foreach (var group in histories.GroupBy(h => h.FundId))
            {
                var lastTwo = group.Take(2).ToList();
                if (lastTwo.Count == 2 && lastTwo[1].Price > 0)
                {
                    var calcChange = Math.Round(((lastTwo[0].Price - lastTwo[1].Price) / lastTwo[1].Price) * 100m, 4);
                    var code = activeFunds.FirstOrDefault(f => f.Id == group.Key)?.Code ?? "";
                    candidates.Add(new { Value = calcChange, Code = code, Id = group.Key });
                }
            }
        }

        var bestFund = candidates
            .OrderByDescending(c => c.Value)
            .FirstOrDefault();

        return new FundStatsDto
        {
            TotalCount = totalCount,
            IslamicCount = islamicCount,
            TotalAum = (double)totalAum,
            BestDailyChange = bestFund != null ? (double?)bestFund.Value : null,
            BestDailyChangeCode = bestFund?.Code
        };
    }

    public async Task<List<FundTypeOptionDto>> GetTypeOptionsAsync(CancellationToken ct = default)
    {
        var types = await _db.FundTypeDefinitions
            .AsNoTracking()
            .Where(t => t.IsVisible && !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.NameAr)
            .ToListAsync(ct);

        return types.Select(t => new FundTypeOptionDto
        {
            Id = t.Id,
            Slug = t.Slug,
            NameAr = t.NameAr,
            NameEn = t.NameEn,
            DisplayOrder = t.DisplayOrder
        }).ToList();
    }

    public async Task<List<FundClassificationTreeDto>> GetClassificationTreeAsync(CancellationToken ct = default)
    {
        var nodes = await _db.FundClassificationNodes
            .AsNoTracking()
            .Where(n => n.IsVisible && !n.IsDeleted)
            .OrderBy(n => n.Level)
            .ThenBy(n => n.DisplayOrder)
            .ThenBy(n => n.NameAr)
            .ToListAsync(ct);

        var funds = await _fundRepo.GetAllAsync(ct);
        var ctx = await _detailQuery.LoadContextAsync(ct);
        var eff = await _detailQuery.ResolveEffectiveManyAsync(funds.Where(f => f.IsActive), ctx);

        var byId = ctx.Nodes.ToDictionary(n => n.Id);

        var direct = new Dictionary<int, int>();
        foreach (var e in eff.Values)
        {
            if (e.NodeId.HasValue)
                direct[e.NodeId.Value] = direct.GetValueOrDefault(e.NodeId.Value) + 1;
        }

        var kids = ctx.Nodes
            .Where(n => n.ParentId.HasValue)
            .GroupBy(n => n.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Id).ToList());

        var total = new Dictionary<int, int>();
        var visiting = new HashSet<int>(); // cycle guard

        int TotalOf(int id)
        {
            if (total.TryGetValue(id, out var cached)) return cached;
            if (!visiting.Add(id)) return 0;

            int t = direct.GetValueOrDefault(id);
            if (kids.TryGetValue(id, out var childIds))
                foreach (var c in childIds) t += TotalOf(c);

            visiting.Remove(id);
            total[id] = t;
            return t;
        }

        foreach (var n in ctx.Nodes.Where(n => n.ParentId == null)) TotalOf(n.Id);

        return nodes
            .Where(n => n.IsVisible && !n.IsDeleted)
            .Select(n => new FundClassificationTreeDto
            {
                Id = n.Id,
                Slug = n.Slug,
                NameAr = n.NameAr,
                NameEn = n.NameEn,
                Level = n.Level,
                ParentSlug = n.ParentId.HasValue && byId.TryGetValue(n.ParentId.Value, out var p) ? p.Slug : null,
                DisplayOrder = n.DisplayOrder,
                ChildrenLabelAr = n.ChildrenLabelAr,
                ChildrenLabelEn = n.ChildrenLabelEn,
                DirectFundCount = direct.GetValueOrDefault(n.Id),
                TotalFundCount = total.GetValueOrDefault(n.Id),
                HasAutoRule = !string.IsNullOrWhiteSpace(n.AutoRuleJson)
            })
            .Where(d => d.TotalFundCount > 0)
            .ToList();
    }

    public async Task<(Dictionary<int, (int TypeId, string Slug)> Overrides, Dictionary<string, Domain.Entities.FundTypeDefinition> BySlug)>
        LoadEffectiveTypesAsync(IReadOnlyCollection<int> fundIds, CancellationToken ct = default)
    {
        var defs = await _db.FundTypeDefinitions.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .ToDictionaryAsync(t => t.Slug, ct);

        var overrides = fundIds.Count == 0
            ? new Dictionary<int, (int, string)>()
            : await (from o in _db.FundTypeOverrides.AsNoTracking()
                     join d in _db.FundTypeDefinitions.AsNoTracking() on o.FundTypeDefinitionId equals d.Id
                     where fundIds.Contains(o.FundId) && !d.IsDeleted
                     select new { o.FundId, d.Id, d.Slug })
                    .ToDictionaryAsync(x => x.FundId, x => (x.Id, x.Slug), ct);

        return (overrides, defs.ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    public async Task<Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto>> GetPricesForFundsAsync(
        IReadOnlyCollection<int> fundIds,
        CancellationToken ct = default)
    {
        if (fundIds == null || fundIds.Count == 0)
        {
            return new Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto>();
        }

        var distinctIds = fundIds.Distinct().ToList();

        return await _db.Funds
            .AsNoTracking()
            .Where(f => distinctIds.Contains(f.Id))
            .Select(f => new Meezan.Application.Common.Models.Funds.FundPriceDto
            {
                Id = f.Id,
                CurrentPrice = f.CurrentPrice,
                DailyChange = f.DailyChange,
                Return1M = f.Return1M,
                Return3M = f.Return3M,
                Return6M = f.Return6M,
                Return1Y = f.Return1Y,
                Return3Y = f.Return3Y,
                PricesUpdatedAt = f.PricesUpdatedAt
            })
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public async Task<List<FundAssetManagerOptionDto>> GetAssetManagersAsync(CancellationToken ct = default)
    {
        return await _db.Funds
            .AsNoTracking()
            .Where(f => f.IsActive && f.AssetManagerId.HasValue && !string.IsNullOrWhiteSpace(f.AssetManagerName))
            .GroupBy(f => new { Id = f.AssetManagerId!.Value, NameAr = f.AssetManagerName, NameEn = f.AssetManagerNameEn })
            .Select(g => new FundAssetManagerOptionDto
            {
                Id = g.Key.Id,
                NameAr = g.Key.NameAr,
                NameEn = g.Key.NameEn,
                FundCount = g.Count()
            })
            .OrderByDescending(m => m.FundCount)
            .ThenBy(m => m.NameAr)
            .ToListAsync(ct);
    }
}

