using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Features.Sectors.DTOs;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meezan.Infrastructure.Persistence.Repositories.Stocks;

public class StockRepository : IStockRepository
{
    private readonly ApplicationDbContext _context;

    public StockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Stock?> GetByIdWithSectorAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Stock?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Sector)
            .Where(s => s.IsActive)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);
    }

    /// <summary>
    /// Like <see cref="GetByTickerAsync"/> but also returns deactivated stocks.
    /// Used by the Excel upload flow so that a previously-deactivated stock with the
    /// same ticker is found and updated (or reactivated) rather than causing a
    /// unique-constraint violation on the Stocks.Ticker column.
    /// </summary>
    public async Task<Stock?> GetByTickerIncludingInactiveAsync(string ticker, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);
    }

    public async Task<Stock?> GetBySymbolCodeAsync(string symbolCode, CancellationToken cancellationToken = default)
    {
        return await GetByTickerIncludingInactiveAsync(symbolCode, cancellationToken);
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

    public async Task<List<Stock>> GetStocksBySectorIdAsync(int sectorId, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.SectorId == sectorId)
            .Include(s => s.MarketData)
            .ToListAsync(cancellationToken);
    }

    public async Task<Stock?> GetByTickerWithMarketDataAsync(string ticker, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Include(s => s.MarketData)
            .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == ticker.Trim().ToUpper(), cancellationToken);
    }

    public async Task<List<Meezan.Application.Features.Scraping.Services.FairValueCalculator.SectorPeerData>> GetSectorPeerDataAsync(
        int sectorId, int excludeStockId, CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .Where(s => s.SectorId == sectorId && s.Id != excludeStockId && s.MarketData != null)
            .Select(s => new Meezan.Application.Features.Scraping.Services.FairValueCalculator.SectorPeerData(
                s.Id,
                s.SectorId,
                s.MarketData!.PeRatio,
                s.MarketData!.PbRatio
            ))
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

        // Weight context: when the request targets a single index (via IndexCode or a
        // single IndexCodes entry), Weight means weight within THAT index — so any
        // weighted index (EGX30, EGX70, Shariah, TAMAYUZ, ...) surfaces its own weights
        // on the sector page as soon as it is selected. Otherwise (sector view with no
        // index filter, or several indices selected) it falls back to Sectoral-Indices.
        // Distinct() guards against a duplicated code counting as "single".
        var weightIndexCode = selectedIndexCodes.Distinct().Count() == 1
            ? selectedIndexCodes[0]
            : "SECTORAL-INDICES";
        bool isSectoral = weightIndexCode.Equals("SECTORAL-INDICES", StringComparison.OrdinalIgnoreCase);

        // Shariah verdict filter � reads the STORED verdict only (no recomputation).
        // Stored Pending = Insufficient data (???????? ??? ?????): neutral, never in Compliant.
        // Legacy aliases: ScholarsCompliant -> Compliant (scholars are normal Compliant now),
        // Insufficient/InsufficientData -> Pending, Doubtful/????? -> Pending (never displayed).
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
                else if (t.Equals("Doubtful", StringComparison.OrdinalIgnoreCase) || t.Contains("?????"))
                    t = "Pending";
                if (Enum.TryParse<ShariahStatus>(t, ignoreCase: true, out var parsed) && !selectedShariahStatuses.Contains(parsed))
                    selectedShariahStatuses.Add(parsed);
            }
        }
        if (f.ShariahStatuses != null) foreach (var s in f.ShariahStatuses) AddShariahToken(s);
        AddShariahToken(f.ShariahStatus);

        if (selectedShariahStatuses.Count > 0)
        {
            // Stored verdict + activity safety net (stored already encodes activity,
            // this covers rows written before the backfill). No board/standards/EGX33 logic.
            query = query.Where(st =>
                selectedShariahStatuses.Contains(
                    (st.ShariahMetrics != null && st.ShariahMetrics.CoreActivityCompliantOverride != null
                        ? (st.ShariahMetrics.CoreActivityCompliantOverride == false ? ShariahStatus.NonCompliant
                            : (st.ShariahCompliance != null ? st.ShariahCompliance.Status : ShariahStatus.Pending))
                        : (st.ShariahMetrics != null && st.ShariahMetrics.CoreActivityCompliant == false
                            ? ShariahStatus.NonCompliant
                            : (st.ShariahCompliance != null ? st.ShariahCompliance.Status : ShariahStatus.Pending)))));
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
                // Halal Bourse entry uses its OWN stored ratio verdict (HalalStatus):
                // Compliant (incl. via scholars) counts as 1; Insufficient is excluded.
                && (st.ShariahSourceOpinions.Count(o =>
                        o.SourceKey != ShariahSourceKey.HalalBourse
                        && o.Status != null && o.Status.ToUpper() == "COMPLIANT")
                    + ((st.ShariahCompliance != null && st.ShariahCompliance.HalalStatus == ShariahStatus.Compliant) ? 1 : 0) >= min));
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
#pragma warning disable CS8602, CS8629 // EF translates the optional navigations in the shariahstatus arm to LEFT JOINs: null-safe in SQL.
            "namear" or "nameAr" => desc ? query.OrderByDescending(x => x.NameAr) : query.OrderBy(x => x.NameAr),
            "nameen" or "nameEn" => desc ? query.OrderByDescending(x => x.NameEn) : query.OrderBy(x => x.NameEn),
            "closingprice" or "closingPrice" => desc ? query.OrderByDescending(x => x.MarketData != null ? x.MarketData.ClosingPrice : null) : query.OrderBy(x => x.MarketData != null ? x.MarketData.ClosingPrice : null),
            "changepct" or "changePct" => desc ? query.OrderByDescending(x => x.SupportResistance != null ? x.SupportResistance.ChangePct : null) : query.OrderBy(x => x.SupportResistance != null ? x.SupportResistance.ChangePct : null),
            "fairvalue" or "fairValue" => desc ? query.OrderByDescending(x => x.FairValue != null ? x.FairValue.FairValue : null) : query.OrderBy(x => x.FairValue != null ? x.FairValue.FairValue : null),
            "pricecomparison" or "priceComparison" => desc ? query.OrderByDescending(x => x.FairValue != null ? (PriceComparison?)x.FairValue.PriceComparison : null) : query.OrderBy(x => x.FairValue != null ? (PriceComparison?)x.FairValue.PriceComparison : null),
            "fairvaluediffpct" or "fairValueDiffPct" => desc ? query.OrderByDescending(x => x.FairValue != null ? x.FairValue.FairValueDiffPct : null) : query.OrderBy(x => x.FairValue != null ? x.FairValue.FairValueDiffPct : null),
            "shariahstatus" or "shariahStatus" => desc ? query.OrderByDescending(x => x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null) : query.OrderBy(x => x.ShariahCompliance != null ? (ShariahStatus?)x.ShariahCompliance.Status : null),
            // Weight in the resolved weightIndexCode.
            // For Sectoral-Indices (or sector views), weights are dynamic and strictly monotonic
            // with effective market cap in EGP (MarketValue * (Currency == 'USD' ? 52.4m : 1.0m)).
            // For other official indices (EGX30, etc.), sort by stored ic.Weight.
            "weight" => isSectoral
                ? (desc
                    ? query.OrderByDescending(x => x.MarketData != null ? x.MarketData.MarketValue * (x.MarketData.Currency == "USD" ? 52.4m : 1.0m) : (decimal?)null)
                    : query.OrderBy(x => x.MarketData != null ? x.MarketData.MarketValue * (x.MarketData.Currency == "USD" ? 52.4m : 1.0m) : (decimal?)null))
                : (desc
                    ? query.OrderByDescending(x => x.IndexConstituents.Where(ic => ic.Index != null && ic.Index.Code.ToUpper() == weightIndexCode).Select(ic => ic.Weight).FirstOrDefault())
                    : query.OrderBy(x => x.IndexConstituents.Where(ic => ic.Index != null && ic.Index.Code.ToUpper() == weightIndexCode).Select(ic => ic.Weight).FirstOrDefault())),
            _ => desc ? query.OrderByDescending(x => x.Ticker) : query.OrderBy(x => x.Ticker)
#pragma warning restore CS8602, CS8629
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
                // HalalBourse rule: compliant + percentage < 100 (or null) is treated as doubtful.
                // Standards-based Bourse Halal upgrade: doubtful Bourse Halal + ≥1 passing
                // quantitative standard (thresholds mirrored from frontend shariah-standards.ts:
                // EGX33 10/33, DFM 10/30, AAOIFI 5/30, S&P 5/33, KLSI 20/33) = Compliant.
#pragma warning disable CS8602, CS8629 // EF translates the optional navigations below to LEFT JOINs: null-safe in SQL, never executed in memory.
                ShariahStatus = st.ShariahCompliance != null ? st.ShariahCompliance.Status.ToString() : null,
                ViaScholarsOnly = st.ShariahCompliance != null ? st.ShariahCompliance.ViaScholarsOnly : false,
                VerdictDetailsJson = st.ShariahCompliance != null ? st.ShariahCompliance.VerdictDetailsJson : null,
                ClosingPrice = st.MarketData != null ? st.MarketData.ClosingPrice : null,
                Currency = st.MarketData != null ? st.MarketData.Currency : null,
                ChangePct = st.SupportResistance != null ? st.SupportResistance.ChangePct : null,
                FairValue = st.FairValue != null ? st.FairValue.FairValue : null,
                PriceComparison = st.FairValue != null ? st.FairValue.PriceComparison.ToString() : null,
                FairValueDiffPct = st.FairValue != null ? st.FairValue.FairValueDiffPct : null,
                SectorNameAr = st.Sector != null ? st.Sector.NameAr : null,
                PeRatio = st.MarketData != null ? st.MarketData.PeRatio : null,
                PbRatio = st.MarketData != null ? st.MarketData.PbRatio : null,
                SectorId = st.SectorId,
                MarketValue = st.MarketData != null ? st.MarketData.MarketValue : null,
                // Weight within the resolved weightIndexCode (null when the stock has no
                // constituent row in that index, e.g. weight-less source files).
                StoredWeight = st.IndexConstituents
                    .Where(ic => ic.Index != null && ic.Index.Code.ToUpper() == weightIndexCode)
                    .Select(ic => ic.Weight)
                    .FirstOrDefault(),
                WeightIndexCode = weightIndexCode,
                IndexCodesList = st.IndexConstituents
                    .Where(ic => ic.Index != null)
                    .Select(ic => ic.Index!.Code)
                    .ToList(),
                // Shariah metrics for non-compliance reason display on the card (effective override ?? feed)
                CoreActivityCompliant = st.ShariahMetrics != null ? (st.ShariahMetrics.CoreActivityCompliantOverride ?? st.ShariahMetrics.CoreActivityCompliant) : null,
                CategoryAr = st.ShariahMetrics != null ? (st.ShariahMetrics.CategoryArOverride ?? st.ShariahMetrics.CategoryAr) : null,
                CategoryEn = st.ShariahMetrics != null ? (st.ShariahMetrics.CategoryEnOverride ?? st.ShariahMetrics.CategoryEn) : null,
                SpHaramEarningPercentage = st.ShariahMetrics != null ? (st.ShariahMetrics.HaramRevenuePercentageOverride ?? st.ShariahMetrics.HaramEarningsPercentage ?? st.ShariahMetrics.SpHaramEarningPercentage) : null,
                LoansPercentage = st.ShariahMetrics != null ? (st.ShariahMetrics.LoansPercentageOverride ?? st.ShariahMetrics.LoansPercentage) : null,
                PurificationPct = st.ShariahCompliance != null ? st.ShariahCompliance.Pct : null
            })
            .ToListAsync(cancellationToken);

        // If weightIndexCode is SECTORAL-INDICES, sector weights are calculated dynamically on the fly
        // using SectorWeightCalculator based on MarketValue (with USD->EGP conversion and 35% cap).
        // For single official indices (EGX30, etc.), stored weights from IndexConstituent are used.
        Dictionary<string, (decimal? Weight, bool IsCapped, decimal CappingFactor)> sectorWeightsByTicker = new(StringComparer.OrdinalIgnoreCase);

        if (isSectoral)
        {
            // Group the page's stocks by SectorId and calculate for each sector
            var distinctSectorIds = pageEntities
                .Where(x => x.SectorId != null)
                .Select(x => x.SectorId!.Value)
                .Distinct()
                .ToList();

            if (distinctSectorIds.Count > 0)
            {
                // Fetch all active stocks in these sectors to ensure full sector denominator and capping accuracy
                var allSectorStocks = await _context.Stocks
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.SectorId != null && distinctSectorIds.Contains(s.SectorId.Value))
                    .Select(s => new
                    {
                        s.Id,
                        s.Ticker,
                        SectorId = s.SectorId!.Value,
                        MarketValue = s.MarketData != null ? s.MarketData.MarketValue : null,
                        Currency = s.MarketData != null ? s.MarketData.Currency : null
                    })
                    .ToListAsync(cancellationToken);

                foreach (var secId in distinctSectorIds)
                {
                    var sectorStocks = allSectorStocks.Where(s => s.SectorId == secId).ToList();
                    var inputs = sectorStocks.Select(s => new Meezan.Application.Common.Services.Market.SectorStockWeightInput(
                        s.Id,
                        s.Ticker,
                        s.MarketValue,
                        s.Currency
                    ));

                    var calcResult = Meezan.Application.Common.Services.Market.SectorWeightCalculator.Calculate(secId, inputs);
                    foreach (var w in calcResult.StockWeights)
                    {
                        sectorWeightsByTicker[w.Ticker] = (w.FinalWeight, w.IsCapped, w.CappingFactor);
                    }
                }
            }
        }

        var items = pageEntities.Select(st =>
        {
            decimal? finalWeight = null;
            bool? isCapped = null;
            decimal? cappingFactor = null;

            if (isSectoral)
            {
                if (sectorWeightsByTicker.TryGetValue(st.Ticker, out var secW))
                {
                    finalWeight = secW.Weight;
                    isCapped = secW.IsCapped;
                    cappingFactor = secW.CappingFactor;
                }
            }
            else
            {
                finalWeight = st.StoredWeight;
            }

            return new StockListProjection(
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
                st.PbRatio,
                finalWeight,
                st.WeightIndexCode,
                isCapped,
                cappingFactor,
                CoreActivityCompliant: st.CoreActivityCompliant,
                CategoryAr: st.CategoryAr,
                CategoryEn: st.CategoryEn,
                SpHaramEarningPercentage: st.SpHaramEarningPercentage,
                LoansPercentage: st.LoansPercentage,
                PurificationPct: st.PurificationPct,
                ViaScholarsOnly: st.ViaScholarsOnly,
                VerdictDetailsJson: st.VerdictDetailsJson
            );
        }).ToList();

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
            // NOTE: Do NOT call SaveChangesAsync here — this method is called
            // from within the upload handler's ambient transaction.  The sector
            // row will be flushed by the handler's own SaveChangesAsync call.
            // Previously calling SaveChanges here could cause a duplicate-key
            // error when two rows race to create the same sector, or promote the
            // local transaction to a distributed transaction on SQL Server.
        }

        return sector;
    }

    public async Task<(Sector? Sector, bool IsNew)> GetOrCreateSectorWithFlagAsync(
        string? nameAr, string? nameEn, CancellationToken cancellationToken = default)
    {
        var cleanAr = nameAr?.Trim();
        var cleanEn = nameEn?.Trim();

        if (string.IsNullOrWhiteSpace(cleanAr) && string.IsNullOrWhiteSpace(cleanEn))
            return (null, false);

        var sector = await _context.Sectors
            .FirstOrDefaultAsync(s =>
                (!string.IsNullOrEmpty(cleanEn) && s.NameEn.ToUpper() == cleanEn.ToUpper()) ||
                (!string.IsNullOrEmpty(cleanAr) && s.NameAr == cleanAr),
                cancellationToken);

        if (sector != null)
            return (sector, false);

        sector = new Sector
        {
            NameAr = cleanAr ?? cleanEn ?? string.Empty,
            NameEn = cleanEn ?? cleanAr ?? string.Empty
        };
        await _context.Sectors.AddAsync(sector, cancellationToken);
        return (sector, true);
    }

    /// <summary>
    /// Returns ALL stocks (active + inactive) with only essential columns loaded.
    /// Used by the Excel-upload flow to build an in-memory Arabic-name lookup table
    /// without issuing one query per file row.
    /// </summary>
    public async Task<List<Stock>> GetAllIncludingInactiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Stocks
            .AsNoTracking()
            .Select(s => new Stock
            {
                Id = s.Id,
                Ticker = s.Ticker,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorId = s.SectorId,
                IsActive = s.IsActive,
                DeactivatedAt = s.DeactivatedAt,
                DeactivationReason = s.DeactivationReason
            })
            .ToListAsync(cancellationToken);
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
    /// EGX33 membership input for overall-verdict recomputes (same weight as a
    /// board opinion). Compares upper-cased codes in SQL: expression trees cannot
    /// call ShariahStatusResolver.IsShariahIndexCode, so the spellings are inlined.
    /// </summary>
    public async Task<bool> IsShariahIndexMemberAsync(int stockId, CancellationToken cancellationToken = default)
    {
        return await _context.IndexConstituents.AsNoTracking().AnyAsync(
            ic => ic.StockId == stockId && ic.Index != null
                && (ic.Index.Code.ToUpper() == "EGX 33" || ic.Index.Code.ToUpper() == "EGX33" || ic.Index.Code.ToUpper() == "SHARIAH"),
            cancellationToken);
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
        // Visible population only (mirrors the stocks-list filters): active stocks
        // with market data. Aggregates are computed in memory through
        // ShariahStatusResolver.EffectiveStatus, so the verdict behind the
        // compliance counts always agrees with the lists and the detail page
        // (activity gate → board opinions → standards upgrade → stored verdict).
        var stocks = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive
                && s.DataStatus == Meezan.Domain.Enums.StockDataStatus.Active
                && s.MarketData != null
                && s.SectorId != null)
            .Include(s => s.Sector)
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.ShariahMetrics)
            .Include(s => s.ShariahCompliance)
            .Include(s => s.ShariahSourceOpinions)
            .ToListAsync(cancellationToken);

        return stocks
            .GroupBy(s => s.SectorId!.Value)
            .Select(g =>
            {
                var sector = g.First().Sector!;
                var list = g.ToList();
                var total = list.Count;
                var compliantCount = 0;
                decimal changeSum = 0;
                var changeN = 0;
                decimal capSum = 0;
                var hasCap = false;
                decimal compCapSum = 0;
                var hasCompCap = false;
                decimal peSum = 0;
                var peN = 0;
                foreach (var st in list)
                {
                    var effective = Meezan.Application.Common.Services.Shariah.ShariahStatusResolver.EffectiveStatus(
                        st.ShariahCompliance, st.ShariahMetrics, st.ShariahSourceOpinions);
                    var isCompliant = effective == Meezan.Domain.Enums.ShariahStatus.Compliant;
                    if (isCompliant) compliantCount++;
                    var chg = st.SupportResistance?.ChangePct;
                    if (chg != null) { changeSum += chg.Value; changeN++; }
                    var cap = st.MarketData?.MarketValue;
                    if (cap != null)
                    {
                        capSum += cap.Value;
                        hasCap = true;
                        if (isCompliant) { compCapSum += cap.Value; hasCompCap = true; }
                    }
                    var pe = st.MarketData?.PeRatio;
                    if (pe != null && pe.Value > 0) { peSum += pe.Value; peN++; }
                }
                return new SectorSummaryDto(
                    sector.Id,
                    sector.NameAr,
                    sector.NameEn,
                    total,
                    compliantCount,
                    total > 0 ? Math.Round(100m * compliantCount / total, 1) : null,
                    changeN > 0 ? changeSum / changeN : null,
                    hasCap ? capSum : null,
                    hasCompCap ? compCapSum : null,
                    peN > 0 ? peSum / peN : null);
            })
            .OrderByDescending(s => s.StocksCount)
            .ToList();
    }
}
