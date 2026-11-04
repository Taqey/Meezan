using System.Text.Json;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
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

    public SectorDailyService(ApplicationDbContext db, ILogger<SectorDailyService> logger)
    {
        _db = db;
        _logger = logger;
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

            // Normalize weights over valid covered items
            decimal sumWeightedReturn = 0m;
            decimal weightRoseSum = 0m;
            var contributors = new List<TopContributorDto>();

            foreach (var item in validItems)
            {
                decimal normalizedWeight = item.RawWeight / potentialWeightSum; // sum = 1.0
                decimal contribPct = normalizedWeight * item.ChangePct; // in percentage points
                sumWeightedReturn += contribPct;

                if (item.ChangePct > 0m)
                {
                    weightRoseSum += normalizedWeight;
                }

                contributors.Add(new TopContributorDto(
                    Stock: item.Ticker,
                    Weight: Math.Round(normalizedWeight * 100m, 2),
                    Change: Math.Round(item.ChangePct, 2),
                    Contribution: Math.Round(contribPct, 4)
                ));
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
            decimal sectorChange = Math.Round(sumWeightedReturn, 2);
            decimal shareOfWeightRose = Math.Round(weightRoseSum * 100m, 2);

            results.Add(new SectorDaily
            {
                SectorId = sector.Id,
                TradingDate = tradingDate,
                ChangePct = sectorChange,
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
