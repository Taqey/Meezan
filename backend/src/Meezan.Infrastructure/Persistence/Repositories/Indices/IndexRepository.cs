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
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Persistence.Repositories.Indices;

public class IndexRepository : IIndexRepository
{
    private readonly ApplicationDbContext _context;

    public IndexRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Index?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Indices.FindAsync([id], cancellationToken);
    }

    public async Task<Index?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .FirstOrDefaultAsync(i => i.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public async Task<List<Index>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AnyAsync(i => i.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
    }

    public Task UpdateAsync(Index index, CancellationToken cancellationToken = default)
    {
        _context.Indices.Update(index);
        return Task.CompletedTask;
    }

    public async Task<IndexConstituent?> GetConstituentAsync(int stockId, int indexId, CancellationToken cancellationToken = default)
    {
        return await _context.IndexConstituents
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);
    }

    public async Task<IndexConstituent?> GetConstituentWithIndexAsync(int stockId, int indexId, CancellationToken cancellationToken = default)
    {
        return await _context.IndexConstituents
            .Include(ic => ic.Index)
            .FirstOrDefaultAsync(ic => ic.StockId == stockId && ic.IndexId == indexId, cancellationToken);
    }

    public async Task AddConstituentAsync(IndexConstituent constituent, CancellationToken cancellationToken = default)
    {
        await _context.IndexConstituents.AddAsync(constituent, cancellationToken);
    }

    public void RemoveConstituent(IndexConstituent constituent)
    {
        _context.IndexConstituents.Remove(constituent);
    }

    public async Task ReplaceConstituentsAsync(int indexId, IEnumerable<IndexConstituent> constituents, CancellationToken cancellationToken = default)
    {
        var existing = await _context.IndexConstituents
            .Where(c => c.IndexId == indexId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            _context.IndexConstituents.RemoveRange(existing);
        }

        await _context.IndexConstituents.AddRangeAsync(constituents, cancellationToken);
    }

    public async Task<HashSet<int>> GetConstituentStockIdsAsync(int indexId, CancellationToken cancellationToken = default)
    {
        var ids = await _context.IndexConstituents.AsNoTracking()
            .Where(c => c.IndexId == indexId)
            .Select(c => c.StockId)
            .ToListAsync(cancellationToken);
        return new HashSet<int>(ids);
    }

    public async Task<List<IndexSummaryProjection>> GetAllSummariesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .AsNoTracking()
            .OrderBy(i => i.Code)
            .Select(i => new IndexSummaryProjection(
                i.Code,
                i.NameAr,
                i.NameEn,
                i.Description,
                i.Constituents.Count(),
                i.LastUpdated
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<ConstituentProjection> Items, int TotalCount)> QueryConstituentsPagedAsync(
        string indexCode, ConstituentListFilter f, CancellationToken cancellationToken = default)
    {
        var upper = indexCode.Trim().ToUpper();

        // Start from IndexConstituents of this index
        var query = _context.IndexConstituents
            .AsNoTracking()
            .Where(ic => ic.Index != null && ic.Index.Code.ToUpper() == upper && ic.Stock != null
                      && ic.Stock.DataStatus == Meezan.Domain.Enums.StockDataStatus.Active);

        // ── Filters ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToUpper();
            query = query.Where(ic =>
                ic.Stock!.Ticker.ToUpper().Contains(s) ||
                (ic.Stock.NameAr != null && ic.Stock.NameAr.Contains(f.Search.Trim())) ||
                (ic.Stock.NameEn != null && ic.Stock.NameEn.ToUpper().Contains(s)));
        }

        // Shariah verdict filter — reads the STORED verdict only (no recomputation).
        var selectedShariahStatuses = new List<Meezan.Domain.Enums.ShariahStatus>();
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
                if (Enum.TryParse<Meezan.Domain.Enums.ShariahStatus>(t, ignoreCase: true, out var parsed) && !selectedShariahStatuses.Contains(parsed))
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
                        ? Meezan.Domain.Enums.ShariahStatus.NonCompliant
                        : (ic.Stock.ShariahCompliance != null ? ic.Stock.ShariahCompliance.Status : Meezan.Domain.Enums.ShariahStatus.Pending))));
        }

        if (!string.IsNullOrWhiteSpace(f.PriceComparison) &&
            Enum.TryParse<Meezan.Domain.Enums.PriceComparison>(f.PriceComparison, ignoreCase: true, out var pc))
        {
            // "Unavailable" also covers stocks with no fair-value row at all: to the
            // user both mean "no computable fair value".
            query = pc == Meezan.Domain.Enums.PriceComparison.Unavailable
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
                // Halal Bourse entry uses its OWN stored ratio verdict (HalalStatus):
                // Compliant (incl. via scholars) counts as 1; Insufficient is excluded.
                && (ic.Stock.ShariahSourceOpinions.Count(o =>
                        o.SourceKey != Meezan.Domain.Enums.ShariahSourceKey.HalalBourse
                        && o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                    + ((ic.Stock.ShariahCompliance != null && ic.Stock.ShariahCompliance.HalalStatus == Meezan.Domain.Enums.ShariahStatus.Compliant) ? 1 : 0) >= min));
        }

        // PE Ratio range filter
        if (f.MinPeRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PeRatio != null && ic.Stock.MarketData.PeRatio >= f.MinPeRatio.Value);
        if (f.MaxPeRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PeRatio != null && ic.Stock.MarketData.PeRatio <= f.MaxPeRatio.Value);

        // PB Ratio range filter
        if (f.MinPbRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PbRatio != null && ic.Stock.MarketData.PbRatio >= f.MinPbRatio.Value);
        if (f.MaxPbRatio.HasValue)
            query = query.Where(ic => ic.Stock!.MarketData != null && ic.Stock.MarketData.PbRatio != null && ic.Stock.MarketData.PbRatio <= f.MaxPbRatio.Value);

        // ── Total count (before pagination, after filtering) ──────────────────
        var totalCount = await query.CountAsync(cancellationToken);

        // ── Sorting ───────────────────────────────────────────────────────────
        bool desc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        // Default sort for constituents is Weight DESC
        if (string.IsNullOrWhiteSpace(f.SortBy))
        {
            query = query.OrderByDescending(x => x.Weight);
        }
        else
        {
            query = (f.SortBy.ToLowerInvariant() switch
            {
#pragma warning disable CS8602, CS8629 // EF translates the optional navigations in the shariahstatus arm to LEFT JOINs: null-safe in SQL.
                "weight"                               => desc ? query.OrderByDescending(x => x.Weight)                                            : query.OrderBy(x => x.Weight),
                "ticker"                               => desc ? query.OrderByDescending(x => x.Stock!.Ticker)                                     : query.OrderBy(x => x.Stock!.Ticker),
                "namear"                               => desc ? query.OrderByDescending(x => x.Stock!.NameAr)                                     : query.OrderBy(x => x.Stock!.NameAr),
                "nameen"                               => desc ? query.OrderByDescending(x => x.Stock!.NameEn)                                     : query.OrderBy(x => x.Stock!.NameEn),
                "closingprice"                         => desc ? query.OrderByDescending(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null) : query.OrderBy(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null),
                "changepct"                            => desc ? query.OrderByDescending(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null) : query.OrderBy(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null),
                "fairvalue"                            => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null),
                "pricecomparison"                      => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? (Meezan.Domain.Enums.PriceComparison?)x.Stock!.FairValue.PriceComparison : null) : query.OrderBy(x => x.Stock!.FairValue != null ? (Meezan.Domain.Enums.PriceComparison?)x.Stock!.FairValue.PriceComparison : null),
                "fairvaluediffpct"                     => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null),
                "shariahstatus"                        => desc ? query.OrderByDescending(x => x.Stock!.ShariahCompliance != null ? (Meezan.Domain.Enums.ShariahStatus?)x.Stock.ShariahCompliance.Status : null) : query.OrderBy(x => x.Stock!.ShariahCompliance != null ? (Meezan.Domain.Enums.ShariahStatus?)x.Stock.ShariahCompliance.Status : null),
                _                                      => query.OrderByDescending(x => x.Weight)
#pragma warning restore CS8602, CS8629
            });
        }

        // ── Pagination ────────────────────────────────────────────────────────
        var pageEntities = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(ic => new
            {
                ic.Stock!.Ticker,
                ic.Stock.NameAr,
                ic.Stock.NameEn,
                // Board opinions win over stored compliance: any board says "compliant" = Compliant.
                // HalalBourse rule: compliant + percentage < 100 (or null) is treated as doubtful.
                // Standards-based Bourse Halal upgrade: doubtful Bourse Halal + ≥1 passing
                // quantitative standard (thresholds mirrored from frontend shariah-standards.ts:
                // EGX33 10/33, DFM 10/30, AAOIFI 5/30, S&P 5/33, KLSI 20/33) = Compliant.
                // Revenue = SpHaramEarningPercentage ?? purification Pct, debt = LoansPercentage.
#pragma warning disable CS8602, CS8629 // EF translates the optional navigations below to LEFT JOINs: null-safe in SQL, never executed in memory.
                ShariahStatus = ic.Stock.ShariahCompliance != null ? ic.Stock.ShariahCompliance.Status.ToString() : null,
#pragma warning restore CS8602, CS8629
                ClosingPrice = ic.Stock.MarketData != null ? ic.Stock.MarketData.ClosingPrice : null,
                Currency = ic.Stock.MarketData != null ? ic.Stock.MarketData.Currency : null,
                ChangePct = ic.Stock.SupportResistance != null ? ic.Stock.SupportResistance.ChangePct : null,
                FairValue = ic.Stock.FairValue != null ? ic.Stock.FairValue.FairValue : null,
                PriceComparison = ic.Stock.FairValue != null ? ic.Stock.FairValue.PriceComparison.ToString() : null,
                FairValueDiffPct = ic.Stock.FairValue != null ? ic.Stock.FairValue.FairValueDiffPct : null,
                ic.Weight,
                SectorNameAr = ic.Stock.Sector != null ? ic.Stock.Sector.NameAr : null,
                HasShariahBoard = ic.Stock.ShariahCompliance != null && ic.Stock.ShariahCompliance.HasShariahBoard,
                IndexCodesList = ic.Stock.IndexConstituents
                    .Where(c => c.Index != null)
                    .Select(c => c.Index!.Code)
                    .ToList(),
                // Activity hard gate: نشاط الشركة غير متوافق is a standalone
                // disqualification, so the source opinions are not read for those rows.
                Opinions = ic.Stock.ShariahSourceOpinions
                    .Where(o =>
                        ic.Stock.ShariahMetrics == null
                        || ic.Stock.ShariahMetrics.CoreActivityCompliant == null
                        || ic.Stock.ShariahMetrics.CoreActivityCompliant == true)
                    .Select(o => new
                    {
                        o.Id,
                        o.StockId,
                        o.SourceKey,
                        o.Status,
                        o.Percentage,
                        o.Note,
                        o.PdfUrl,
                        o.SourceLastUpdated,
                        o.FetchedAt,
                        o.ExtraData
                    }).ToList()
            })
            .ToListAsync(cancellationToken);

        var items = pageEntities.Select(p => new ConstituentProjection(
            p.Ticker,
            p.NameAr,
            p.NameEn,
            string.Join("|", p.IndexCodesList),
            p.ShariahStatus,
            p.Opinions.Select(o => new Meezan.Application.Features.Shariah.DTOs.ShariahSourceOpinionDto
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
            }).ToList(),
            p.ClosingPrice,
            p.ChangePct,
            p.FairValue,
            p.PriceComparison,
            p.FairValueDiffPct,
            p.Weight,
            p.Currency,
            p.SectorNameAr,
            p.HasShariahBoard
        )).ToList();

        return (items, totalCount);
    }
}
