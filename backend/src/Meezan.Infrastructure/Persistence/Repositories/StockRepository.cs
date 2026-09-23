using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Sectors.Queries.GetSectorsList;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories;

public class StockRepository : IStockRepository
{
    private readonly ApplicationDbContext _context;

    public StockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);
    }

    public async Task<Stock?> GetBySymbolCodeAsync(string symbolCode, CancellationToken cancellationToken = default)
    {
        return await GetByTickerAsync(symbolCode, cancellationToken);
    }

    public async Task<List<Stock>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Sector)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Stock>> GetAllWithMarketDataAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Include(s => s.MarketData)
            .Include(s => s.FairValue)
                .ThenInclude(fv => fv!.Methods)
            .Include(s => s.SupportResistance)
            .ToListAsync(cancellationToken);
    }

    public async Task<Stock?> GetByTickerWithFullDetailsAsync(
        string ticker, CancellationToken cancellationToken = default)
    {
        var upper = ticker.Trim().ToUpper();
        return await _context.Stocks
            .Include(s => s.Sector)
            .Include(s => s.IndexConstituents)
                .ThenInclude(ic => ic.Index)
            .Include(s => s.ShariahCompliance)
            .Include(s => s.ShariahSourceOpinions)
            .Include(s => s.ShariahMetrics)
            .Include(s => s.MarketData)
            .Include(s => s.FairValue)
                .ThenInclude(fv => fv!.Methods)
            .Include(s => s.SupportResistance)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == upper, cancellationToken);
    }

    public async Task<(List<StockListProjection> Items, int TotalCount)> QueryPagedAsync(
        StockListFilter f, CancellationToken cancellationToken = default)
    {
        // Start from Stocks — all joins are LEFT (via EF optional navigation)
        var query = _context.Stocks.AsNoTracking();

        // ── Filters ───────────────────────────────────────────────────────────

        // Only show Active stocks — IncompleteNoData and Suspended are hidden from the public frontend.
        // Stocks are automatically marked IncompleteNoData by the scraper when Mubasher returns no data.
        query = query.Where(st => st.DataStatus == Meezan.Domain.Enums.StockDataStatus.Active);

        // Exclude stocks with no StockMarketData record — these are real stocks
        // that Mubasher hasn't yet published a trading page for (e.g. recent IPOs).
        // Once the daily scraper successfully scrapes a stock, it creates a MarketData
        // row and the stock automatically appears here — no manual intervention needed.
        query = query.Where(st => st.MarketData != null);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToUpper();
            query = query.Where(st =>
                st.Ticker.ToUpper().Contains(s) ||
                (st.NameAr != null && st.NameAr.Contains(f.Search.Trim())) ||
                (st.NameEn != null && st.NameEn.ToUpper().Contains(s)));
        }

        if (f.SectorId.HasValue)
            query = query.Where(st => st.SectorId == f.SectorId.Value);

        if (!string.IsNullOrWhiteSpace(f.IndexCode))
        {
            var code = f.IndexCode.Trim().ToUpper();
            query = query.Where(st =>
                st.IndexConstituents.Any(ic => ic.Index!.Code.ToUpper() == code));
        }

        if (!string.IsNullOrWhiteSpace(f.ShariahStatus) &&
            Enum.TryParse<ShariahStatus>(f.ShariahStatus, ignoreCase: true, out var shStatus))
        {
            query = query.Where(st =>
                st.ShariahCompliance != null && st.ShariahCompliance.Status == shStatus);
        }

        if (!string.IsNullOrWhiteSpace(f.PriceComparison) &&
            Enum.TryParse<PriceComparison>(f.PriceComparison, ignoreCase: true, out var pc))
        {
            query = query.Where(st =>
                st.FairValue != null && st.FairValue.PriceComparison == pc);
        }

        if (f.MinCompliantSources.HasValue && f.MinCompliantSources.Value > 0)
        {
            var min = f.MinCompliantSources.Value;
            query = query.Where(st =>
                st.ShariahSourceOpinions.Count(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT") >= min);
        }

        // ── Total count (before pagination, after filtering) ──────────────────
        var totalCount = await query.CountAsync(cancellationToken);

        // ── Sorting ───────────────────────────────────────────────────────────
        bool desc = string.Equals(f.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = (f.SortBy?.ToLowerInvariant() switch
        {
            "namear" or "nameAr"                   => desc ? query.OrderByDescending(x => x.NameAr)                                    : query.OrderBy(x => x.NameAr),
            "nameen" or "nameEn"                   => desc ? query.OrderByDescending(x => x.NameEn)                                    : query.OrderBy(x => x.NameEn),
            "closingprice" or "closingPrice"       => desc ? query.OrderByDescending(x => x.MarketData != null ? x.MarketData.ClosingPrice : null) : query.OrderBy(x => x.MarketData != null ? x.MarketData.ClosingPrice : null),
            "changepct" or "changePct"             => desc ? query.OrderByDescending(x => x.SupportResistance != null ? x.SupportResistance.ChangePct : null) : query.OrderBy(x => x.SupportResistance != null ? x.SupportResistance.ChangePct : null),
            "fairvalue" or "fairValue"             => desc ? query.OrderByDescending(x => x.FairValue != null ? x.FairValue.FairValue : null) : query.OrderBy(x => x.FairValue != null ? x.FairValue.FairValue : null),
            "pricecomparison" or "priceComparison" => desc ? query.OrderByDescending(x => x.FairValue != null ? (PriceComparison?)x.FairValue.PriceComparison : null) : query.OrderBy(x => x.FairValue != null ? (PriceComparison?)x.FairValue.PriceComparison : null),
            "fairvaluediffpct" or "fairValueDiffPct"=> desc ? query.OrderByDescending(x => x.FairValue != null ? x.FairValue.FairValueDiffPct : null) : query.OrderBy(x => x.FairValue != null ? x.FairValue.FairValueDiffPct : null),
            "shariahstatus" or "shariahStatus"     => desc ? query.OrderByDescending(x => x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null) : query.OrderBy(x => x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null),
            _                                      => desc ? query.OrderByDescending(x => x.Ticker)                                    : query.OrderBy(x => x.Ticker)
        });

        // ── Pagination ────────────────────────────────────────────────────────
        var pageEntities = await query
            .Skip((f.Page - 1) * f.PageSize)
            .Take(f.PageSize)
            .Select(st => new
            {
                st.Id,
                st.Ticker,
                st.NameAr,
                st.NameEn,
                ShariahStatus = st.ShariahCompliance != null ? st.ShariahCompliance.Status.ToString() : null,
                ClosingPrice = st.MarketData != null ? st.MarketData.ClosingPrice : null,
                Currency = st.MarketData != null ? st.MarketData.Currency : null,
                ChangePct = st.SupportResistance != null ? st.SupportResistance.ChangePct : null,
                FairValue = st.FairValue != null ? st.FairValue.FairValue : null,
                PriceComparison = st.FairValue != null ? st.FairValue.PriceComparison.ToString() : null,
                FairValueDiffPct = st.FairValue != null ? st.FairValue.FairValueDiffPct : null,
                SectorNameAr = st.Sector != null ? st.Sector.NameAr : null,
                IndexCodesList = st.IndexConstituents
                    .Where(ic => ic.Index != null)
                    .Select(ic => ic.Index!.Code)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var items = pageEntities.Select(st => new StockListProjection(
            st.Ticker,
            st.NameAr,
            st.NameEn,
            string.Join("|", st.IndexCodesList),
            st.ShariahStatus,
            st.ClosingPrice,
            st.ChangePct,
            st.FairValue,
            st.PriceComparison,
            st.FairValueDiffPct,
            st.Currency,
            st.SectorNameAr
        )).ToList();

        return (items, totalCount);
    }

    public async Task<Sector?> GetOrCreateSectorAsync(string? nameAr, string? nameEn, CancellationToken cancellationToken = default)
    {
        var cleanAr = nameAr?.Trim();
        var cleanEn = nameEn?.Trim();

        if (string.IsNullOrWhiteSpace(cleanAr) && string.IsNullOrWhiteSpace(cleanEn))
        {
            return null;
        }

        var sector = await _context.Sectors
            .FirstOrDefaultAsync(s =>
                (!string.IsNullOrEmpty(cleanEn) && s.NameEn.ToUpper() == cleanEn.ToUpper()) ||
                (!string.IsNullOrEmpty(cleanAr) && s.NameAr == cleanAr),
                cancellationToken);

        if (sector == null)
        {
            sector = new Sector
            {
                NameAr = cleanAr ?? cleanEn ?? string.Empty,
                NameEn = cleanEn ?? cleanAr ?? string.Empty
            };
            await _context.Sectors.AddAsync(sector, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sector;
    }

    public async Task AddAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        await _context.Stocks.AddAsync(stock, cancellationToken);
    }

    public Task UpdateAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        _context.Stocks.Update(stock);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePermanentlyAsync(int stockId, CancellationToken cancellationToken = default)
    {
        // Use a transaction so partial failure leaves the DB untouched.
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete in FK order (children before parents).
                // ExecuteDeleteAsync bypasses the change tracker — safe here because we
                // have no tracked entities for this stock at call time.

                await _context.ShariahSourceOpinions
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.ShariahCompliances
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.StockShariahMetrics
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.IndexConstituents
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                // StockFairValueMethods → StockFairValues (cascade-delete via FK,
                // but explicit here to be safe with the ExecuteDeleteAsync path).
                await _context.StockFairValueMethods
                    .Where(m => m.StockFairValue!.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.StockFairValues
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.StockSupportResistance
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.StockMarketData
                    .Where(x => x.StockId == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.Stocks
                    .Where(s => s.Id == stockId)
                    .ExecuteDeleteAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }


    public async Task<List<SectorSummaryDto>> GetSectorsWithCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Sectors
            .AsNoTracking()
            .Where(s => s.Stocks.Any())
            .OrderByDescending(s => s.Stocks.Count)
            .Select(s => new SectorSummaryDto(
                s.Id,
                s.NameAr,
                s.NameEn,
                s.Stocks.Count))
            .ToListAsync(cancellationToken);
    }
}
