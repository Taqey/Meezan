using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Queries.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Models.Caching;
using Meezan.Application.Common.Models.Indices;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Queries.Indices;

/// <summary>
/// Set-based constituents query: static data (names, weights, index codes) comes from
/// <see cref="IIndexStaticCache"/>; price/market/verdict fields are read fresh for the page's stock
/// ids in ONE query (+ ONE query for stored Shariah opinions) and merged in memory by id. Filters or
/// sorts that depend on market/verdict data first resolve the page ids in SQL (count + ids).
/// Never more than 6 SQL commands, independent of the number of constituents. Read-only: stored
/// Shariah results are read, never recomputed.
/// </summary>
public sealed class IndexConstituentReadQuery : IIndexConstituentReadQuery
{
    private readonly ApplicationDbContext _db;
    private readonly IIndexStaticCache _staticCache;

    public IndexConstituentReadQuery(ApplicationDbContext db, IIndexStaticCache staticCache)
    {
        _db = db;
        _staticCache = staticCache;
    }

    public async Task<IndexConstituentsPage?> QueryPageAsync(
        string indexCode, ConstituentListFilter f, CancellationToken ct = default)
    {
        var entry = await _staticCache.GetAsync(indexCode, ct);
        if (entry == null) return null;

        var byId = new Dictionary<int, IndexStaticRow>();
        foreach (var r in entry.Rows) byId.TryAdd(r.StockId, r);

        List<int> pageIds;
        int total;
        if (IsStaticOnly(f))
            (pageIds, total) = PageFromStatic(entry, f);
        else
            (pageIds, total) = await QueryFilteredIdsAsync(entry.IndexId, f, ct);

        var (fresh, opinions) = await LoadFreshAsync(pageIds, ct);

        var items = new List<ConstituentProjection>(pageIds.Count);
        foreach (var id in pageIds)
        {
            if (!byId.TryGetValue(id, out var st)) continue;
            fresh.TryGetValue(id, out var fr);
            opinions.TryGetValue(id, out var ops);
            items.Add(new ConstituentProjection(
                st.Ticker,
                st.NameAr,
                st.NameEn,
                string.Join("|", st.IndexCodes),
                fr?.ShariahStatus,
                ops ?? new List<ShariahSourceOpinionDto>(),
                fr?.ClosingPrice,
                fr?.ChangePct,
                fr?.FairValue,
                fr?.PriceComparison,
                fr?.FairValueDiffPct,
                st.Weight,
                fr?.Currency,
                st.SectorNameAr,
                fr?.HasShariahBoard ?? false));
        }

        return new IndexConstituentsPage(entry.Code, entry.NameAr, entry.NameEn, entry.LastUpdated, items, total);
    }

    // ── Static-only path (search / weight / ticker / name sorts) ────────────────────────────────

    private static bool IsStaticOnly(ConstituentListFilter f)
    {
        if (!string.IsNullOrWhiteSpace(f.ShariahStatus)) return false;
        if (f.ShariahStatuses is { Length: > 0 }) return false;
        if (!string.IsNullOrWhiteSpace(f.PriceComparison)) return false;
        if (f.MinCompliantSources is > 0) return false;
        if (f.MinPeRatio.HasValue || f.MaxPeRatio.HasValue || f.MinPbRatio.HasValue || f.MaxPbRatio.HasValue) return false;
        var sort = (f.SortBy ?? string.Empty).Trim().ToLowerInvariant();
        return sort is "" or "weight" or "ticker" or "namear" or "nameen";
    }

    private static (List<int> Ids, int Total) PageFromStatic(IndexStaticEntry entry, ConstituentListFilter f)
    {
        IEnumerable<IndexStaticRow> q = entry.Rows;
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(r =>
                r.Ticker.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.NameAr != null && r.NameAr.Contains(s, StringComparison.OrdinalIgnoreCase))
                || (r.NameEn != null && r.NameEn.Contains(s, StringComparison.OrdinalIgnoreCase)));
        }

        bool desc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var sort = (f.SortBy ?? string.Empty).Trim().ToLowerInvariant();
        var cmp = StringComparer.OrdinalIgnoreCase;
        IOrderedEnumerable<IndexStaticRow> ordered = sort switch
        {
            "weight" => desc ? q.OrderByDescending(x => x.Weight) : q.OrderBy(x => x.Weight),
            "ticker" => desc ? q.OrderByDescending(x => x.Ticker, cmp) : q.OrderBy(x => x.Ticker, cmp),
            "namear" => desc ? q.OrderByDescending(x => x.NameAr, cmp) : q.OrderBy(x => x.NameAr, cmp),
            "nameen" => desc ? q.OrderByDescending(x => x.NameEn, cmp) : q.OrderBy(x => x.NameEn, cmp),
            _ => q.OrderByDescending(x => x.Weight)
        };

        var list = ordered.ThenBy(x => x.Ticker, StringComparer.Ordinal).ToList();
        var ids = list.Skip((f.Page - 1) * f.PageSize).Take(f.PageSize).Select(x => x.StockId).ToList();
        return (ids, list.Count);
    }

    // ── DB path: filters/sorts on market or verdict data. 2 commands (count + ids). ─────────────

    private async Task<(List<int> Ids, int Total)> QueryFilteredIdsAsync(
        int indexId, ConstituentListFilter f, CancellationToken ct)
    {
        var query = _db.IndexConstituents
            .AsNoTracking()
            .Where(ic => ic.IndexId == indexId && ic.Stock != null
                      && ic.Stock.DataStatus == StockDataStatus.Active);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToUpper();
            query = query.Where(ic =>
                ic.Stock!.Ticker.ToUpper().Contains(s) ||
                (ic.Stock.NameAr != null && ic.Stock.NameAr.Contains(f.Search.Trim())) ||
                (ic.Stock.NameEn != null && ic.Stock.NameEn.ToUpper().Contains(s)));
        }

        // Shariah verdict filter — reads the STORED verdict only (no recomputation).
        var selectedShariahStatuses = new List<ShariahStatus>();
        void AddShariahToken(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Equals("ScholarsCompliant", StringComparison.OrdinalIgnoreCase))
                    t = "Compliant";
                else if (t.Equals("Insufficient", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("InsufficientData", StringComparison.OrdinalIgnoreCase)
                    || t.Equals("Insufficient_Data", StringComparison.OrdinalIgnoreCase))
                    t = "Pending";
                else if (t.Equals("Doubtful", StringComparison.OrdinalIgnoreCase) || t.Contains("\u0645\u0634\u0643\u0648\u0643"))
                    t = "Pending";
                if (Enum.TryParse<ShariahStatus>(t, ignoreCase: true, out var parsed) && !selectedShariahStatuses.Contains(parsed))
                    selectedShariahStatuses.Add(parsed);
            }
        }
        if (f.ShariahStatuses != null) foreach (var s in f.ShariahStatuses) AddShariahToken(s);
        AddShariahToken(f.ShariahStatus);

        if (selectedShariahStatuses.Count > 0)
        {
            query = query.Where(ic =>
                selectedShariahStatuses.Contains(
                    (ic.Stock!.ShariahMetrics != null && ic.Stock.ShariahMetrics.CoreActivityCompliant == false
                        ? ShariahStatus.NonCompliant
                        : (ic.Stock.ShariahCompliance != null ? ic.Stock.ShariahCompliance.Status : ShariahStatus.Pending))));
        }

        if (!string.IsNullOrWhiteSpace(f.PriceComparison) &&
            Enum.TryParse<PriceComparison>(f.PriceComparison, ignoreCase: true, out var pc))
        {
            query = pc == PriceComparison.Unavailable
                ? query.Where(ic => ic.Stock!.FairValue == null || ic.Stock!.FairValue.PriceComparison == pc)
                : query.Where(ic => ic.Stock!.FairValue != null && ic.Stock!.FairValue.PriceComparison == pc);
        }

        if (f.MinCompliantSources.HasValue && f.MinCompliantSources.Value > 0)
        {
            var min = f.MinCompliantSources.Value;
            query = query.Where(ic =>
                (ic.Stock!.ShariahMetrics == null
                 || ic.Stock.ShariahMetrics.CoreActivityCompliant == null
                 || ic.Stock.ShariahMetrics.CoreActivityCompliant == true)
                && (ic.Stock.ShariahSourceOpinions.Count(o =>
                        o.SourceKey != ShariahSourceKey.HalalBourse
                        && o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                    + ((ic.Stock.ShariahCompliance != null && ic.Stock.ShariahCompliance.HalalStatus == ShariahStatus.Compliant) ? 1 : 0) >= min));
        }

        if (f.MinPeRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PeRatio != null && ic.Stock.MarketData.PeRatio >= f.MinPeRatio.Value);
        if (f.MaxPeRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PeRatio != null && ic.Stock.MarketData.PeRatio <= f.MaxPeRatio.Value);
        if (f.MinPbRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PbRatio != null && ic.Stock.MarketData.PbRatio >= f.MinPbRatio.Value);
        if (f.MaxPbRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PbRatio != null && ic.Stock.MarketData.PbRatio <= f.MaxPbRatio.Value);

        var totalCount = await query.CountAsync(ct);

        bool desc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(f.SortBy))
        {
            query = query.OrderByDescending(x => x.Weight);
        }
        else
        {
            query = (f.SortBy.ToLowerInvariant() switch
            {
#pragma warning disable CS8602, CS8629 // EF translates the optional navigations to LEFT JOINs: null-safe in SQL.
                "weight" => desc ? query.OrderByDescending(x => x.Weight) : query.OrderBy(x => x.Weight),
                "ticker" => desc ? query.OrderByDescending(x => x.Stock!.Ticker) : query.OrderBy(x => x.Stock!.Ticker),
                "namear" => desc ? query.OrderByDescending(x => x.Stock!.NameAr) : query.OrderBy(x => x.Stock!.NameAr),
                "nameen" => desc ? query.OrderByDescending(x => x.Stock!.NameEn) : query.OrderBy(x => x.Stock!.NameEn),
                "closingprice" => desc ? query.OrderByDescending(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null) : query.OrderBy(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null),
                "changepct" => desc ? query.OrderByDescending(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null) : query.OrderBy(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null),
                "fairvalue" => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null),
                "pricecomparison" => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? (PriceComparison?)x.Stock!.FairValue.PriceComparison : null) : query.OrderBy(x => x.Stock!.FairValue != null ? (PriceComparison?)x.Stock!.FairValue.PriceComparison : null),
                "fairvaluediffpct" => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null),
                "shariahstatus" => desc ? query.OrderByDescending(x => x.Stock!.ShariahCompliance != null ? (ShariahStatus?)x.Stock.ShariahCompliance.Status : null) : query.OrderBy(x => x.Stock!.ShariahCompliance != null ? (ShariahStatus?)x.Stock.ShariahCompliance.Status : null),
                _ => query.OrderByDescending(x => x.Weight)
#pragma warning restore CS8602, CS8629
            });
        }

        var ids = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(ic => ic.StockId)
            .ToListAsync(ct);

        return (ids, totalCount);
    }

    // ── Fresh data: ONE query for market/verdict fields + ONE for stored opinions ───────────────

    private sealed class FreshRow
    {
        public int Id { get; init; }
        public string? ShariahStatus { get; init; }
        public decimal? ClosingPrice { get; init; }
        public string? Currency { get; init; }
        public decimal? ChangePct { get; init; }
        public decimal? FairValue { get; init; }
        public string? PriceComparison { get; init; }
        public decimal? FairValueDiffPct { get; init; }
        public bool HasShariahBoard { get; init; }
        public bool CoreOk { get; init; }
    }

    private async Task<(Dictionary<int, FreshRow> Fresh, Dictionary<int, List<ShariahSourceOpinionDto>> Opinions)> LoadFreshAsync(
        List<int> ids, CancellationToken ct)
    {
        var fresh = new Dictionary<int, FreshRow>();
        var opinions = new Dictionary<int, List<ShariahSourceOpinionDto>>();
        if (ids.Count == 0) return (fresh, opinions);

#pragma warning disable CS8602, CS8629 // EF translates the optional navigations to LEFT JOINs: null-safe in SQL.
        var rows = await _db.Stocks.AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .Select(s => new FreshRow
            {
                Id = s.Id,
                ShariahStatus = s.ShariahCompliance != null ? s.ShariahCompliance.Status.ToString() : null,
                ClosingPrice = s.MarketData != null ? s.MarketData.ClosingPrice : null,
                Currency = s.MarketData != null ? s.MarketData.Currency : null,
                ChangePct = s.SupportResistance != null ? s.SupportResistance.ChangePct : null,
                FairValue = s.FairValue != null ? s.FairValue.FairValue : null,
                PriceComparison = s.FairValue != null ? s.FairValue.PriceComparison.ToString() : null,
                FairValueDiffPct = s.FairValue != null ? s.FairValue.FairValueDiffPct : null,
                HasShariahBoard = s.ShariahCompliance != null && s.ShariahCompliance.HasShariahBoard,
                // Activity hard gate: non-compliant core activity is a standalone disqualification,
                // so the source opinions are not read for those rows.
                CoreOk = s.ShariahMetrics == null
                         || s.ShariahMetrics.CoreActivityCompliant == null
                         || s.ShariahMetrics.CoreActivityCompliant == true
            })
            .ToListAsync(ct);
#pragma warning restore CS8602, CS8629

        foreach (var r in rows) fresh[r.Id] = r;

        var coreIds = rows.Where(r => r.CoreOk).Select(r => r.Id).ToList();
        if (coreIds.Count > 0)
        {
            var ops = await _db.ShariahSourceOpinions.AsNoTracking()
                .Where(o => coreIds.Contains(o.StockId))
                .OrderBy(o => o.Id)
                .Select(o => new ShariahSourceOpinionDto
                {
                    Id = o.Id,
                    StockId = o.StockId,
                    SourceKey = o.SourceKey,
                    Status = o.Status,
                    Percentage = o.Percentage,
                    Note = o.Note,
                    PdfUrl = o.PdfUrl,
                    SourceLastUpdated = o.SourceLastUpdated,
                    FetchedAt = o.FetchedAt,
                    ExtraData = o.ExtraData
                })
                .ToListAsync(ct);

            foreach (var o in ops)
            {
                if (!opinions.TryGetValue(o.StockId, out var list))
                    opinions[o.StockId] = list = new List<ShariahSourceOpinionDto>();
                list.Add(o);
            }
        }

        return (fresh, opinions);
    }
}
