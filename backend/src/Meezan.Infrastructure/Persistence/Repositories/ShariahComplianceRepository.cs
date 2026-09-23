using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class ShariahComplianceRepository : IShariahComplianceRepository
{
    private readonly ApplicationDbContext _context;

    public ShariahComplianceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShariahCompliance?> GetByStockIdAsync(int stockId, CancellationToken cancellationToken = default)
    {
        return await _context.ShariahCompliances
            .Include(c => c.Stock)
            .FirstOrDefaultAsync(c => c.StockId == stockId, cancellationToken);
    }

    public async Task<ShariahCompliance?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var upper = symbol.ToUpperInvariant();
        return await _context.ShariahCompliances
            .Include(c => c.Stock)
            .FirstOrDefaultAsync(c => c.Stock != null && c.Stock.Ticker == upper, cancellationToken);
    }

    public async Task<List<ShariahCompliance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ShariahCompliances
            .Include(c => c.Stock)
            .OrderBy(c => c.Stock != null ? c.Stock.Ticker : string.Empty)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<ShariahCompliance> Items, int TotalCount)> QueryPagedAsync(
        ShariahListFilter f, CancellationToken cancellationToken = default)
    {
        var query = _context.ShariahCompliances
            .Include(c => c.Stock)
            .AsNoTracking();

        // ── Filters ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToUpper();
            query = query.Where(c =>
                c.Stock != null && (
                    c.Stock.Ticker.ToUpper().Contains(s) ||
                    (c.Stock.NameAr != null && c.Stock.NameAr.Contains(f.Search.Trim())) ||
                    (c.Stock.NameEn != null && c.Stock.NameEn.ToUpper().Contains(s))
                ));
        }

        if (!string.IsNullOrWhiteSpace(f.Status) &&
            Enum.TryParse<Meezan.Domain.Enums.ShariahStatus>(f.Status, ignoreCase: true, out var statusVal))
        {
            query = query.Where(c => c.Status == statusVal);
        }

        // ── Total count (before pagination, after filtering) ──────────────────
        var totalCount = await query.CountAsync(cancellationToken);

        // ── Sorting ───────────────────────────────────────────────────────────
        bool desc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = (f.SortBy?.ToLowerInvariant() switch
        {
            "symbolcode" or "ticker" => desc ? query.OrderByDescending(c => c.Stock != null ? c.Stock.Ticker : string.Empty) : query.OrderBy(c => c.Stock != null ? c.Stock.Ticker : string.Empty),
            "stocknamear" or "namear" => desc ? query.OrderByDescending(c => c.Stock != null ? c.Stock.NameAr : string.Empty) : query.OrderBy(c => c.Stock != null ? c.Stock.NameAr : string.Empty),
            "stocknameen" or "nameen" => desc ? query.OrderByDescending(c => c.Stock != null ? c.Stock.NameEn : string.Empty) : query.OrderBy(c => c.Stock != null ? c.Stock.NameEn : string.Empty),
            "status"                 => desc ? query.OrderByDescending(c => c.Status)                                          : query.OrderBy(c => c.Status),
            "pct"                    => desc ? query.OrderByDescending(c => c.Pct)                                             : query.OrderBy(c => c.Pct),
            "lastcheckedat"          => desc ? query.OrderByDescending(c => c.LastCheckedAt)                                   : query.OrderBy(c => c.LastCheckedAt),
            "updatedat"              => desc ? query.OrderByDescending(c => c.UpdatedAt)                                       : query.OrderBy(c => c.UpdatedAt),
            _                        => query.OrderBy(c => c.Stock != null ? c.Stock.Ticker : string.Empty)
        });

        // ── Pagination ────────────────────────────────────────────────────────
        var items = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(ShariahCompliance compliance, CancellationToken cancellationToken = default)
    {
        await _context.ShariahCompliances.AddAsync(compliance, cancellationToken);
    }

    public void Update(ShariahCompliance compliance)
    {
        _context.ShariahCompliances.Update(compliance);
    }
}
