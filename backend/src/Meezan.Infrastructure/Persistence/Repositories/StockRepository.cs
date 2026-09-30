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
            .Where(s => s.IsActive)
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
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Stock>> GetAllWithMarketDataAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
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
            .Where(s => s.IsActive)
            .Include(s => s.Sector)
            .Include(s => s.IndexConstituents)
                .ThenInclude(ic => ic.Index)
            .Include(s => s.ShariahCompliance)
            // Activity hard gate: when نشاط الشركة is itself prohibited the stock is out
            // at the first screening gate, so its source opinions are not read at all.
            // (Predicate goes through the child's own navigation — EF cannot translate the
            // parent lambda parameter inside a filtered Include.)
            // ShariahMetrics MUST be included BEFORE the filtered include on ShariahSourceOpinions,
            // because the filter predicate references o.Stock.ShariahMetrics.
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahSourceOpinions.Where(o =>
                o.Stock.ShariahMetrics == null
                || o.Stock.ShariahMetrics.CoreActivityCompliant == null
                || o.Stock.ShariahMetrics.CoreActivityCompliant == true))
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
        var query = _context.Stocks.AsNoTracking().Where(st => st.IsActive);

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

        // Index filter: support both single IndexCode and multiple IndexCodes (OR logic among selected)
        var selectedIndexCodes = new List<string>();
        if (f.IndexCodes != null && f.IndexCodes.Length > 0)
        {
            selectedIndexCodes.AddRange(f.IndexCodes
                .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(c => c.Trim().ToUpper())
                .Where(c => !string.IsNullOrEmpty(c)));
        }
        if (!string.IsNullOrWhiteSpace(f.IndexCode))
        {
            var single = f.IndexCode.Trim().ToUpper();
            if (!selectedIndexCodes.Contains(single))
                selectedIndexCodes.Add(single);
        }
        if (selectedIndexCodes.Count > 0)
        {
            query = query.Where(st =>
                st.IndexConstituents.Any(ic => selectedIndexCodes.Contains(ic.Index!.Code.ToUpper())));
        }

        // Shariah status filter: support both single ShariahStatus and multiple ShariahStatuses (OR logic)
        var selectedShariahStatuses = new List<ShariahStatus>();
        if (f.ShariahStatuses != null && f.ShariahStatuses.Length > 0)
        {
            foreach (var raw in f.ShariahStatuses.SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries)))
            {
                if (Enum.TryParse<ShariahStatus>(raw.Trim(), ignoreCase: true, out var parsed) && !selectedShariahStatuses.Contains(parsed))
                    selectedShariahStatuses.Add(parsed);
            }
        }
        if (!string.IsNullOrWhiteSpace(f.ShariahStatus) &&
            Enum.TryParse<ShariahStatus>(f.ShariahStatus.Trim(), ignoreCase: true, out var singleStatus) &&
            !selectedShariahStatuses.Contains(singleStatus))
        {
            selectedShariahStatuses.Add(singleStatus);
        }

        if (selectedShariahStatuses.Count > 0)
        {
            // Effective status = (1) activity gate → (2) any board "compliant" → (3) stored verdict.
            query = query.Where(st =>
                selectedShariahStatuses.Contains(
                    (st.ShariahMetrics != null && st.ShariahMetrics.CoreActivityCompliant == false
                        ? ShariahStatus.NonCompliant
                        : (st.ShariahSourceOpinions.Any(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                            ? ShariahStatus.Compliant
                            : (st.ShariahCompliance != null ? st.ShariahCompliance.Status : (ShariahStatus)(-1))))
                ));
        }

        if (!string.IsNullOrWhiteSpace(f.PriceComparison) &&
            Enum.TryParse<PriceComparison>(f.PriceComparison, ignoreCase: true, out var pc))
        {
            // "Unavailable" also covers stocks with no fair-value row at all: to the
            // user both mean "no computable fair value".
            query = pc == PriceComparison.Unavailable
                ? query.Where(st => st.FairValue == null || st.FairValue.PriceComparison == pc)
                : query.Where(st => st.FairValue != null && st.FairValue.PriceComparison == pc);
        }

        if (f.MinCompliantSources.HasValue && f.MinCompliantSources.Value > 0)
        {
            var min = f.MinCompliantSources.Value;
            query = query.Where(st =>
                // Activity hard gate: an activity-non-compliant stock has its source
                // opinions suppressed on the detail page and in index constituents, so it
                // must not match an opinions-based filter either. Condition reads only
                // this stock's own activity flag (never the sector's).
                (st.ShariahMetrics == null
                 || st.ShariahMetrics.CoreActivityCompliant == null
                 || st.ShariahMetrics.CoreActivityCompliant == true)
                && st.ShariahSourceOpinions.Count(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT") >= min);
        }

        // PE Ratio range filter
        if (f.MinPeRatio.HasValue)
            query = query.Where(st => st.MarketData != null && st.MarketData.PeRatio != null && st.MarketData.PeRatio >= f.MinPeRatio.Value);
        if (f.MaxPeRatio.HasValue)
            query = query.Where(st => st.MarketData != null && st.MarketData.PeRatio != null && st.MarketData.PeRatio <= f.MaxPeRatio.Value);

        // PB Ratio range filter
        if (f.MinPbRatio.HasValue)
            query = query.Where(st => st.MarketData != null && st.MarketData.PbRatio != null && st.MarketData.PbRatio >= f.MinPbRatio.Value);
        if (f.MaxPbRatio.HasValue)
            query = query.Where(st => st.MarketData != null && st.MarketData.PbRatio != null && st.MarketData.PbRatio <= f.MaxPbRatio.Value);

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
            "shariahstatus" or "shariahStatus"     => desc ? query.OrderByDescending(x => x.ShariahMetrics != null && x.ShariahMetrics.CoreActivityCompliant == false ? (ShariahStatus?)ShariahStatus.NonCompliant : (x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null))                                                                                   : query.OrderBy(x => x.ShariahMetrics != null && x.ShariahMetrics.CoreActivityCompliant == false ? (ShariahStatus?)ShariahStatus.NonCompliant : (x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null)),
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
                // Single source of truth (ShariahStatusResolver.EffectiveStatus, inlined):
                // the stock's own activity result wins over its stored ratio verdict, so
                // this list and the stock-detail page can never disagree.
                // Board opinions win over stored compliance: any board says "compliant" = Compliant.
                ShariahStatus = st.ShariahMetrics != null && st.ShariahMetrics.CoreActivityCompliant == false
                    ? ShariahStatus.NonCompliant.ToString()
                    : (st.ShariahSourceOpinions.Any(o => o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                        ? ShariahStatus.Compliant.ToString()
                        : (st.ShariahCompliance != null
                            ? st.ShariahCompliance.Status.ToString()
                            : null)),
                ClosingPrice = st.MarketData != null ? st.MarketData.ClosingPrice : null,
                Currency = st.MarketData != null ? st.MarketData.Currency : null,
                ChangePct = st.SupportResistance != null ? st.SupportResistance.ChangePct : null,
                FairValue = st.FairValue != null ? st.FairValue.FairValue : null,
                PriceComparison = st.FairValue != null ? st.FairValue.PriceComparison.ToString() : null,
                FairValueDiffPct = st.FairValue != null ? st.FairValue.FairValueDiffPct : null,
                SectorNameAr = st.Sector != null ? st.Sector.NameAr : null,
                PeRatio = st.MarketData != null ? st.MarketData.PeRatio : null,
                PbRatio = st.MarketData != null ? st.MarketData.PbRatio : null,
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
            st.SectorNameAr,
            st.PeRatio,
            st.PbRatio
        )).ToList();

        return (items, totalCount);
    }

    public async Task<List<Stock>> GetByTickersIncludingInactiveAsync(
        IEnumerable<string> tickers, CancellationToken cancellationToken = default)
    {
        var upper = tickers
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpper())
            .Distinct()
            .ToList();

        if (upper.Count == 0) return new List<Stock>();

        // NOTE: deliberately NO IsActive filter — the operator refresh must be able
        // to target deactivated/flagged stocks. Tracking query (no AsNoTracking) so
        // the caller can persist DataStatus restorations on success.
        return await _context.Stocks
            .Include(s => s.Sector)
            .Include(s => s.MarketData)
            .Where(s => upper.Contains(s.Ticker.ToUpper()))
            .ToListAsync(cancellationToken);
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

    /// <summary>
    /// Marks the given stocks Active again after a successful operator-driven
    /// re-scrape. Uses ExecuteUpdate (single SQL, no change tracking) so the
    /// entity graph held by the caller is never marked Modified.
    /// </summary>
    public async Task SetActiveDataStatusAsync(
        IEnumerable<int> stockIds, CancellationToken cancellationToken = default)
    {
        var ids = stockIds.Distinct().ToList();
        if (ids.Count == 0) return;

        await _context.Stocks
            .Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.DataStatus, StockDataStatus.Active)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow),
            cancellationToken);
    }

    public async Task DeactivateAsync(int stockId, string reason, CancellationToken cancellationToken = default)
    {
        // Soft-deactivation — the row and all child data stay in the DB.
        // IsActive = false hides the stock from all normal read queries.
        await _context.Stocks
            .Where(s => s.Id == stockId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsActive, false)
                .SetProperty(s => s.DeactivatedAt, DateTime.UtcNow)
                .SetProperty(s => s.DeactivationReason, reason)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow),
            cancellationToken);
    }

    public async Task ReactivateAsync(int stockId, CancellationToken cancellationToken = default)
    {
        await _context.Stocks
            .Where(s => s.Id == stockId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsActive, true)
                .SetProperty(s => s.DeactivatedAt, (DateTime?)null)
                .SetProperty(s => s.DeactivationReason, (string?)null)
                .SetProperty(s => s.UpdatedAt, DateTime.UtcNow),
            cancellationToken);
    }

    public async Task<List<Stock>> GetDeactivatedStocksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => !s.IsActive)
            .Include(s => s.MarketData)
            .Include(s => s.Sector)
            .OrderByDescending(s => s.DeactivatedAt)
            .ToListAsync(cancellationToken);
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
