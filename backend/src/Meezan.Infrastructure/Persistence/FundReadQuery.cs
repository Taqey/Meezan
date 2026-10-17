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
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence;

public sealed class FundReadQuery : IFundReadQuery
{
    private readonly ApplicationDbContext _db;
    private readonly IFundRepository _fundRepo;
    private readonly Services.FundDetailQuery _detailQuery;

    public FundReadQuery(
        ApplicationDbContext db,
        IFundRepository fundRepo,
        Services.FundDetailQuery detailQuery)
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

        var bestFund = await q
            .Where(f => f.DailyChange != null)
            .OrderByDescending(f => f.DailyChange)
            .Select(f => new { f.DailyChange, f.Code })
            .FirstOrDefaultAsync(ct);

        return new FundStatsDto
        {
            TotalCount = totalCount,
            IslamicCount = islamicCount,
            TotalAum = (double)totalAum,
            BestDailyChange = bestFund != null ? (double?)bestFund.DailyChange : null,
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
}
