using Meezan.Application.Common.Interfaces.Caching;
using System.Text.Json;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Services.Market;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using Meezan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Jobs;

public class SectorDailyService : ISectorDailyService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SectorDailyService> _logger;

    public const string UsdEgpRateKey = "USD_EGP_RATE";
    public const string WeightingMethodKey = "SECTOR_CHANGE_WEIGHTING";
    public const string ExternalSectorPullKey = "EXTERNAL_SECTOR_PULL_ENABLED";

    public const decimal DefaultUsdEgpRate = 55.35m;
    public const string DefaultWeightingMethod = "marketCap"; // marketCap | displayedWeight

    private readonly IIndexStaticCache? _indexStaticCache;

    public SectorDailyService(ApplicationDbContext db, ILogger<SectorDailyService> logger, IIndexStaticCache? indexStaticCache = null)
    {
        _db = db;
        _logger = logger;
        _indexStaticCache = indexStaticCache;
    }

    public async Task<decimal> GetUsdEgpRateAsync(CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == UsdEgpRateKey, ct);
        if (setting != null && decimal.TryParse(setting.Value, out var val) && val > 0)
        {
            return val;
        }
        return DefaultUsdEgpRate;
    }

    public async Task SetUsdEgpRateAsync(decimal rate, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == UsdEgpRateKey, ct);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = UsdEgpRateKey,
                Value = rate.ToString("F4"),
                Description = "USD to EGP exchange rate for market cap weighting",
                UpdatedAt = DateTime.UtcNow
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = rate.ToString("F4");
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> GetWeightingMethodAsync(CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == WeightingMethodKey, ct);
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
        {
            return setting.Value.Trim();
        }
        return DefaultWeightingMethod;
    }

    public async Task SetWeightingMethodAsync(string method, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == WeightingMethodKey, ct);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = WeightingMethodKey,
                Value = method.Trim(),
                Description = "Sector daily change weighting: marketCap | displayedWeight",
                UpdatedAt = DateTime.UtcNow
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = method.Trim();
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsExternalSectorPullEnabledAsync(CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == ExternalSectorPullKey, ct);
        if (setting != null && bool.TryParse(setting.Value, out var enabled))
        {
            return enabled;
        }
        return false; // Default false as required
    }

    public async Task SetExternalSectorPullEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == ExternalSectorPullKey, ct);
        if (setting == null)
        {
            setting = new SystemSetting
            {
                Key = ExternalSectorPullKey,
                Value = enabled.ToString().ToLowerInvariant(),
                Description = "Flag to enable/disable external Mubasher sector quotes scraping",
                UpdatedAt = DateTime.UtcNow
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = enabled.ToString().ToLowerInvariant();
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<SectorDaily>> RecalculateSectorsAsync(List<int>? affectedSectorIds, string reason, CancellationToken ct = default)
    {
        _logger.LogInformation("RecalculateSectorsAsync invoked. Reason: {Reason}, AffectedSectors: {Sectors}",
            reason, affectedSectorIds != null ? string.Join(",", affectedSectorIds) : "ALL");

        // 1. Resolve trading date (latest from snapshots or current date)
        var latestSnapshotDate = await _db.StockDailySnapshots
            .OrderByDescending(s => s.TradingDate)
            .Select(s => (DateOnly?)s.TradingDate)
            .FirstOrDefaultAsync(ct);

        DateOnly tradingDate;
        if (latestSnapshotDate.HasValue)
        {
            tradingDate = latestSnapshotDate.Value;
        }
        else
        {
            var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
            tradingDate = DateOnly.FromDateTime(nowCairo);
        }

        // 2. Synchronize MarketValue for active stocks whose MarketValue may be outdated vs ClosingPrice * SharesOutstanding
        var activeStocksQuery = _db.Stocks
            .Include(s => s.MarketData)
            .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active);

        if (affectedSectorIds != null && affectedSectorIds.Count > 0)
        {
            activeStocksQuery = activeStocksQuery.Where(s => s.SectorId != null && affectedSectorIds.Contains(s.SectorId.Value));
        }

        var activeStocks = await activeStocksQuery.ToListAsync(ct);
        bool mdChanged = false;

        foreach (var st in activeStocks)
        {
            if (st.MarketData != null && st.MarketData.SharesOutstanding.HasValue && st.MarketData.SharesOutstanding.Value > 0)
            {
                // If closing price is missing on MarketData, check latest snapshot for price
                var price = st.MarketData.ClosingPrice;
                if (!price.HasValue)
                {
                    price = _db.StockDailySnapshots
                        .Where(snap => snap.StockId == st.Id && snap.Price.HasValue && snap.Price.Value > 0)
                        .OrderByDescending(snap => snap.TradingDate)
                        .Select(snap => snap.Price)
                        .FirstOrDefault();

                    if (price.HasValue)
                    {
                        st.MarketData.ClosingPrice = price;
                        mdChanged = true;
                    }
                }

                if (price.HasValue && price.Value > 0)
                {
                    var expectedMv = price.Value * st.MarketData.SharesOutstanding.Value;
                    if (st.MarketData.MarketValue != expectedMv)
                    {
                        st.MarketData.MarketValue = expectedMv;
                        mdChanged = true;
                    }
                }
            }
        }

        if (mdChanged)
        {
            await _db.SaveChangesAsync(ct);
        }

        // 3. Synchronize today's StockDailySnapshots in bulk:
        // Query active stock IDs across the system once
        var allActiveStockInfo = await _db.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active)
            .Select(s => new { s.Id, s.SectorId, Shares = s.MarketData != null ? s.MarketData.SharesOutstanding : null })
            .ToDictionaryAsync(s => s.Id, ct);

        var existingTodaySnaps = await _db.StockDailySnapshots
            .Where(s => s.TradingDate == tradingDate)
            .ToListAsync(ct);

        bool snapsChanged = false;
        foreach (var snap in existingTodaySnaps)
        {
            if (allActiveStockInfo.TryGetValue(snap.StockId, out var activeInfo))
            {
                if (activeInfo.Shares != null && snap.SharesUsed != activeInfo.Shares)
                {
                    snap.SharesUsed = activeInfo.Shares;
                    snapsChanged = true;
                }
                if (snap.SectorId != activeInfo.SectorId)
                {
                    snap.SectorId = activeInfo.SectorId;
                    snapsChanged = true;
                }
            }
            else
            {
                // Stock is no longer active: remove snapshot for this trading date
                _db.StockDailySnapshots.Remove(snap);
                snapsChanged = true;
            }
        }

        if (snapsChanged)
        {
            await _db.SaveChangesAsync(ct);
        }

        // 4. Run calculation for the affected sectors (or all if null)
        var results = await CalculateAsync(
            targetTradingDate: tradingDate,
            specificSectorIds: affectedSectorIds,
            triggeredBy: reason,
            ct: ct);

        // 5. Invalidate caches (IndexStaticCache covers SECTORAL-INDICES and all index constituents)
        if (_indexStaticCache != null)
        {
            try
            {
                await _indexStaticCache.InvalidateAsync("SECTORAL-INDICES", ct);
                await _indexStaticCache.InvalidateAsync(null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating index cache during RecalculateSectorsAsync");
            }
        }

        return results;
    }

    public async Task<List<SectorDaily>> CalculateAsync(
        DateOnly? targetTradingDate = null,
        List<int>? specificSectorIds = null,
        string triggeredBy = "Job",
        CancellationToken ct = default)
    {
        _logger.LogInformation("SectorDailyService.CalculateAsync triggered by '{TriggeredBy}' for date: {Date}",
            triggeredBy, targetTradingDate?.ToString("yyyy-MM-dd") ?? "latest");

        // 1. Resolve trading date if not passed
        DateOnly tradingDate;
        if (targetTradingDate.HasValue)
        {
            tradingDate = targetTradingDate.Value;
        }
        else
        {
            var dateFromSnapshots = await _db.StockDailySnapshots
                .OrderByDescending(s => s.TradingDate)
                .Select(s => (DateOnly?)s.TradingDate)
                .FirstOrDefaultAsync(ct);

            if (dateFromSnapshots.HasValue)
            {
                tradingDate = dateFromSnapshots.Value;
            }
            else
            {
                var nowCairo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
                tradingDate = DateOnly.FromDateTime(nowCairo);
            }
        }

        // 2. Load settings
        var usdEgpRate = await GetUsdEgpRateAsync(ct);
        var weightingMethod = await GetWeightingMethodAsync(ct);

        // 3. Check if we have snapshots for this trading date
        var snapshotsForDate = await _db.StockDailySnapshots
            .AsNoTracking()
            .Where(s => s.TradingDate == tradingDate)
            .ToListAsync(ct);

        // If no snapshots exist for trading date, take current stocks state and persist snapshots!
        if (snapshotsForDate.Count == 0)
        {
            var liveStocks = await _db.Stocks
                .AsNoTracking()
                .Include(s => s.MarketData)
                .Include(s => s.SupportResistance)
                .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active)
                .ToListAsync(ct);

            var newSnapshots = new List<StockDailySnapshot>();
            foreach (var stock in liveStocks)
            {
                var md = stock.MarketData;
                var sr = stock.SupportResistance;
                var price = md?.ClosingPrice;
                var chgPct = sr?.ChangePct;
                decimal? prevClose = null;
                if (price.HasValue && chgPct.HasValue)
                {
                    var denom = 1m + (chgPct.Value / 100m);
                    if (denom != 0m)
                    {
                        prevClose = Math.Round(price.Value / denom, 4);
                    }
                }

                newSnapshots.Add(new StockDailySnapshot
                {
                    StockId = stock.Id,
                    Ticker = stock.Ticker,
                    TradingDate = tradingDate,
                    Price = price,
                    PrevClose = prevClose,
                    ChangePct = chgPct,
                    SharesUsed = md?.SharesOutstanding,
                    Currency = md?.Currency,
                    SectorId = stock.SectorId,
                    RecordedAt = DateTime.UtcNow
                });
            }

            if (newSnapshots.Count > 0)
            {
                _db.StockDailySnapshots.AddRange(newSnapshots);
                await _db.SaveChangesAsync(ct);
                snapshotsForDate = newSnapshots;
            }
        }

        // 4. Load current active sectors & active stocks membership (membership is read dynamically)
        var sectorsQuery = _db.Sectors.AsNoTracking();
        if (specificSectorIds != null && specificSectorIds.Count > 0)
        {
            sectorsQuery = sectorsQuery.Where(s => specificSectorIds.Contains(s.Id));
        }
        var sectors = await sectorsQuery.ToListAsync(ct);

        var currentStocks = await _db.Stocks
            .AsNoTracking()
            .Include(s => s.MarketData)
            .Include(s => s.SupportResistance)
            .Include(s => s.IndexConstituents)
            .Where(s => s.IsActive && s.DataStatus == StockDataStatus.Active)
            .ToListAsync(ct);

        var snapshotMap = snapshotsForDate.ToDictionary(s => s.StockId);

        // 5. Look up previous trading day snapshots to check shares outstanding changes without corporate action
        var prevTradingDate = await _db.StockDailySnapshots
            .AsNoTracking()
            .Where(s => s.TradingDate < tradingDate)
            .OrderByDescending(s => s.TradingDate)
            .Select(s => (DateOnly?)s.TradingDate)
            .FirstOrDefaultAsync(ct);

        Dictionary<int, StockDailySnapshot>? prevSnapshotsMap = null;
        if (prevTradingDate.HasValue)
        {
            var prevList = await _db.StockDailySnapshots
                .AsNoTracking()
                .Where(s => s.TradingDate == prevTradingDate.Value)
                .ToListAsync(ct);
            prevSnapshotsMap = prevList.ToDictionary(s => s.StockId);
        }

        var results = new List<SectorDaily>();

        foreach (var sector in sectors)
        {
            // Read CURRENT membership for this sector
            var sectorStocks = currentStocks.Where(s => s.SectorId == sector.Id).ToList();
            int stocksTotal = sectorStocks.Count;

            var validItems = new List<StockCalcItem>();
            decimal totalSectorMarketCap = 0m;
            decimal potentialWeightSum = 0m;

            foreach (var stock in sectorStocks)
            {
                snapshotMap.TryGetValue(stock.Id, out var snap);

                var price = snap?.Price ?? stock.MarketData?.ClosingPrice;
                var chgPct = snap?.ChangePct ?? stock.SupportResistance?.ChangePct;
                var shares = snap?.SharesUsed ?? stock.MarketData?.SharesOutstanding;
                var curr = snap?.Currency ?? stock.MarketData?.Currency;

                // Check USD conversion
                var isUsd = (curr != null && curr.Contains("دولار", StringComparison.OrdinalIgnoreCase)) ||
                            (stock.MarketData?.Currency != null && stock.MarketData.Currency.Contains("دولار", StringComparison.OrdinalIgnoreCase));
                var currencyFactor = isUsd ? usdEgpRate : 1.0m;

                // Add to sector total market cap (in EGP)
                if (price.HasValue && price.Value > 0 && shares.HasValue && shares.Value > 0)
                {
                    totalSectorMarketCap += price.Value * shares.Value * currencyFactor;
                }

                // Check corporate action check: shares changed vs previous day without price adjustment
                if (prevSnapshotsMap != null && prevSnapshotsMap.TryGetValue(stock.Id, out var prevSnap))
                {
                    if (shares.HasValue && prevSnap.SharesUsed.HasValue &&
                        shares.Value != prevSnap.SharesUsed.Value &&
                        price.HasValue && prevSnap.Price.HasValue &&
                        price.Value == prevSnap.Price.Value && (chgPct ?? 0m) == 0m)
                    {
                        _logger.LogWarning("Corporate action detected for {Ticker}: shares changed from {Old} to {New} with unadjusted price. Excluded for {Date}.",
                            stock.Ticker, prevSnap.SharesUsed, shares, tradingDate);
                        continue;
                    }
                }

                // Exclude: No price, or no shares, or shares <= 0
                if (!price.HasValue || price.Value <= 0 || !shares.HasValue || shares.Value <= 0)
                {
                    continue;
                }

                decimal r_i;
                decimal prevClose_i;

                if (snap?.PrevClose.HasValue == true && snap.PrevClose.Value > 0)
                {
                    prevClose_i = snap.PrevClose.Value;
                    r_i = (price.Value / prevClose_i) - 1.0m;
                }
                else if (chgPct.HasValue)
                {
                    r_i = chgPct.Value / 100m;
                    var denom = 1.0m + r_i;
                    prevClose_i = denom != 0m ? Math.Round(price.Value / denom, 4) : price.Value;
                }
                else
                {
                    // Suspended stock (no trade today): r = 0, keep its weight
                    r_i = 0m;
                    prevClose_i = price.Value;
                }

                decimal rawWeight;
                if (weightingMethod.Equals("displayedWeight", StringComparison.OrdinalIgnoreCase))
                {
                    // Stored index constituent or sector weight
                    var constituent = stock.IndexConstituents.FirstOrDefault(c =>
                        c.Index != null && (c.Index.Code == "Sectoral-Indices" || c.Index.Code == "SECTORAL-INDICES"));
                    rawWeight = constituent?.Weight ?? 0m;
                    if (rawWeight <= 0) rawWeight = 1m;
                }
                else
                {
                    // marketCap: w_i = prevClose_i * sharesOutstanding_i * currencyFactor
                    rawWeight = prevClose_i * shares.Value * currencyFactor;
                }

                potentialWeightSum += rawWeight;

                validItems.Add(new StockCalcItem(
                    stock.Ticker,
                    rawWeight,
                    r_i,
                    r_i * 100m,
                    price.Value
                ));
            }

            // Compute capped weights using the existing SectorWeightCalculator
            // Pass the active sector stocks' effective market values (with currency) and the configured usdEgpRate
            var weightInputs = sectorStocks.Select(s =>
            {
                snapshotMap.TryGetValue(s.Id, out var snap);
                var price = snap?.Price ?? s.MarketData?.ClosingPrice;
                var shares = snap?.SharesUsed ?? s.MarketData?.SharesOutstanding;
                var curr = snap?.Currency ?? s.MarketData?.Currency;
                decimal? mv = (price.HasValue && shares.HasValue && price.Value > 0 && shares.Value > 0)
                    ? price.Value * shares.Value
                    : (s.MarketData?.MarketValue);
                return new SectorStockWeightInput(s.Id, s.Ticker, mv, curr);
            });
            var cappedWeightResult = SectorWeightCalculator.Calculate(sector.Id, weightInputs, usdEgpRate);
            var cappedWeightMap = cappedWeightResult.StockWeights.ToDictionary(w => w.Ticker, w => w.FinalWeight ?? 0m);

            int stocksUsed = validItems.Count;
            int advancers = validItems.Count(i => i.ChangePct > 0m);
            int decliners = validItems.Count(i => i.ChangePct < 0m);
            int unchanged = validItems.Count(i => i.ChangePct == 0m);

            if (stocksUsed == 0 || potentialWeightSum <= 0)
            {
                // Store null when no stock has data
                results.Add(new SectorDaily
                {
                    SectorId = sector.Id,
                    TradingDate = tradingDate,
                    ChangePct = null,
                    ChangePctUncapped = null,
                    TotalMarketCap = totalSectorMarketCap,
                    Advancers = 0,
                    Decliners = 0,
                    Unchanged = 0,
                    StocksUsed = 0,
                    StocksTotal = stocksTotal,
                    CoveragePct = 0m,
                    Approximate = true,
                    ShareOfWeightRose = 0m,
                    TopContributorsJson = "[]",
                    MethodUsed = weightingMethod,
                    ComputedAt = DateTime.UtcNow
                });
                continue;
            }

            // 1. Uncapped calculation (kept as sector_return_uncapped)
            decimal sumUncappedWeightedReturn = 0m;
            foreach (var item in validItems)
            {
                decimal normalizedWeight = item.RawWeight / potentialWeightSum; // sum = 1.0
                decimal contribPct = normalizedWeight * item.ChangePct;
                sumUncappedWeightedReturn += contribPct;
            }

            // 2. Capped calculation using existing capped weights:
            // sector_return_capped = sum(stock_change_% * capped_weight) / sum(capped_weight)
            decimal sumCappedWeights = 0m;
            decimal sumCappedWeightedReturn = 0m;
            decimal weightRoseSum = 0m;
            var contributors = new List<TopContributorDto>();

            foreach (var item in validItems)
            {
                cappedWeightMap.TryGetValue(item.Ticker, out var cappedW); // e.g. 35.0, 9.28
                sumCappedWeights += cappedW;
                decimal itemContrib = cappedW * item.ChangePct; // weighted contribution
                sumCappedWeightedReturn += itemContrib;

                if (item.ChangePct > 0m)
                {
                    weightRoseSum += cappedW;
                }

                contributors.Add(new TopContributorDto(
                    Stock: item.Ticker,
                    Weight: Math.Round(cappedW, 2),
                    Change: Math.Round(item.ChangePct, 2),
                    Contribution: sumCappedWeights > 0 ? Math.Round(itemContrib / 100m, 4) : 0m
                ));
            }

            decimal? sectorChangeCapped = null;
            if (sumCappedWeights > 0)
            {
                sectorChangeCapped = Math.Round(sumCappedWeightedReturn / sumCappedWeights, 2);
            }
            decimal sectorChangeUncapped = Math.Round(sumUncappedWeightedReturn, 2);

            // Re-normalize contributors contribution to sum to sectorChangeCapped
            if (sumCappedWeights > 0)
            {
                for (int ci = 0; ci < contributors.Count; ci++)
                {
                    var c = contributors[ci];
                    var contribNorm = (c.Weight / sumCappedWeights) * c.Change;
                    contributors[ci] = c with { Contribution = Math.Round(contribNorm, 4) };
                }
            }

            // Top 3 contributors sorted descending by absolute contribution
            var top3 = contributors
                .OrderByDescending(c => Math.Abs(c.Contribution))
                .Take(3)
                .ToList();

            decimal coveragePct = stocksTotal > 0
                ? Math.Round(((decimal)stocksUsed / stocksTotal) * 100m, 2)
                : 100m;

            bool approximate = coveragePct < 70m;
            decimal shareOfWeightRose = sumCappedWeights > 0 ? Math.Round((weightRoseSum / sumCappedWeights) * 100m, 2) : 0m;

            results.Add(new SectorDaily
            {
                SectorId = sector.Id,
                TradingDate = tradingDate,
                ChangePct = sectorChangeCapped,
                ChangePctUncapped = sectorChangeUncapped,
                TotalMarketCap = Math.Round(totalSectorMarketCap, 2),
                Advancers = advancers,
                Decliners = decliners,
                Unchanged = unchanged,
                StocksUsed = stocksUsed,
                StocksTotal = stocksTotal,
                CoveragePct = coveragePct,
                Approximate = approximate,
                ShareOfWeightRose = shareOfWeightRose,
                TopContributorsJson = JsonSerializer.Serialize(top3),
                MethodUsed = weightingMethod,
                ComputedAt = DateTime.UtcNow
            });

        }

        // 6. Upsert into database
        foreach (var row in results)
        {
            var existing = await _db.SectorDailies
                .FirstOrDefaultAsync(s => s.SectorId == row.SectorId && s.TradingDate == row.TradingDate, ct);

            if (existing != null)
            {
                existing.ChangePct = row.ChangePct;
                existing.ChangePctUncapped = row.ChangePctUncapped;
                existing.TotalMarketCap = row.TotalMarketCap;
                existing.Advancers = row.Advancers;
                existing.Decliners = row.Decliners;
                existing.Unchanged = row.Unchanged;
                existing.StocksUsed = row.StocksUsed;
                existing.StocksTotal = row.StocksTotal;
                existing.CoveragePct = row.CoveragePct;
                existing.Approximate = row.Approximate;
                existing.ShareOfWeightRose = row.ShareOfWeightRose;
                existing.TopContributorsJson = row.TopContributorsJson;
                existing.MethodUsed = row.MethodUsed;
                existing.ComputedAt = DateTime.UtcNow;
            }
            else
            {
                _db.SectorDailies.Add(row);
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Successfully saved {Count} SectorDaily rows for {Date}", results.Count, tradingDate);

        return results;
    }

    public async Task<int> RecomputeDateRangeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        _logger.LogInformation("Recomputing SectorDaily date range from {From} to {To}", fromDate, toDate);

        var distinctDates = await _db.StockDailySnapshots
            .AsNoTracking()
            .Where(s => s.TradingDate >= fromDate && s.TradingDate <= toDate)
            .Select(s => s.TradingDate)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync(ct);

        if (distinctDates.Count == 0)
        {
            distinctDates = new List<DateOnly> { toDate };
        }

        int count = 0;
        foreach (var date in distinctDates)
        {
            var rows = await CalculateAsync(date, specificSectorIds: null, triggeredBy: "DateRangeRecompute", ct: ct);
            count += rows.Count;
        }

        return count;
    }

    public async Task<List<SectorDaily>> GetLatestPerSectorAsync(CancellationToken ct = default)
    {
        // One query to get latest row for each sector
        var latestDate = await _db.SectorDailies
            .MaxAsync(s => (DateOnly?)s.TradingDate, ct);

        if (!latestDate.HasValue) return new List<SectorDaily>();

        return await _db.SectorDailies
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.TradingDate == latestDate.Value)
            .ToListAsync(ct);
    }

    public async Task<SectorDaily?> GetLatestForSectorAsync(int sectorId, CancellationToken ct = default)
    {
        return await _db.SectorDailies
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.SectorId == sectorId)
            .OrderByDescending(s => s.TradingDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<SectorDaily>> GetHistoryForSectorAsync(int sectorId, int maxDays = 30, CancellationToken ct = default)
    {
        return await _db.SectorDailies
            .AsNoTracking()
            .Where(s => s.SectorId == sectorId)
            .OrderByDescending(s => s.TradingDate)
            .Take(maxDays)
            .ToListAsync(ct);
    }

    private record StockCalcItem(
        string Ticker,
        decimal RawWeight,
        decimal ReturnRate,
        decimal ChangePct,
        decimal Price
    );
}
