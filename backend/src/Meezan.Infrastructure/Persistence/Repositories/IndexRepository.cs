using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Index = Meezan.Domain.Entities.Index;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class IndexRepository : IIndexRepository
{
    private readonly ApplicationDbContext _context;

    public IndexRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Index?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Indices
            .FirstOrDefaultAsync(i => i.Code.ToUpper() == code.Trim().ToUpper(), cancellationToken);
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

        if (!string.IsNullOrWhiteSpace(f.ShariahStatus) &&
            Enum.TryParse<Meezan.Domain.Enums.ShariahStatus>(f.ShariahStatus, ignoreCase: true, out var shStatus))
        {
            query = query.Where(ic =>
                ic.Stock!.ShariahCompliance != null && ic.Stock.ShariahCompliance.Status == shStatus);
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
                "weight"                               => desc ? query.OrderByDescending(x => x.Weight)                                            : query.OrderBy(x => x.Weight),
                "ticker"                               => desc ? query.OrderByDescending(x => x.Stock!.Ticker)                                     : query.OrderBy(x => x.Stock!.Ticker),
                "namear"                               => desc ? query.OrderByDescending(x => x.Stock!.NameAr)                                     : query.OrderBy(x => x.Stock!.NameAr),
                "nameen"                               => desc ? query.OrderByDescending(x => x.Stock!.NameEn)                                     : query.OrderBy(x => x.Stock!.NameEn),
                "closingprice"                         => desc ? query.OrderByDescending(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null) : query.OrderBy(x => x.Stock!.MarketData != null ? x.Stock!.MarketData.ClosingPrice : null),
                "changepct"                            => desc ? query.OrderByDescending(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null) : query.OrderBy(x => x.Stock!.SupportResistance != null ? x.Stock!.SupportResistance.ChangePct : null),
                "fairvalue"                            => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValue : null),
                "pricecomparison"                      => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? (Meezan.Domain.Enums.PriceComparison?)x.Stock!.FairValue.PriceComparison : null) : query.OrderBy(x => x.Stock!.FairValue != null ? (Meezan.Domain.Enums.PriceComparison?)x.Stock!.FairValue.PriceComparison : null),
                "fairvaluediffpct"                     => desc ? query.OrderByDescending(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null) : query.OrderBy(x => x.Stock!.FairValue != null ? x.Stock!.FairValue.FairValueDiffPct : null),
                "shariahstatus"                        => desc ? query.OrderByDescending(x => x.Stock!.ShariahCompliance != null ? (Meezan.Domain.Enums.ShariahStatus?)x.Stock!.ShariahCompliance.Status : null) : query.OrderBy(x => x.Stock!.ShariahCompliance != null ? (Meezan.Domain.Enums.ShariahStatus?)x.Stock!.ShariahCompliance.Status : null),
                _                                      => query.OrderByDescending(x => x.Weight)
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
                ShariahStatus = ic.Stock.ShariahCompliance != null ? ic.Stock.ShariahCompliance.Status.ToString() : null,
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
                Opinions = ic.Stock.ShariahSourceOpinions.Select(o => new
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
