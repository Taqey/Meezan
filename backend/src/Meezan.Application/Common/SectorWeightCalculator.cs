using System;
using System.Collections.Generic;
using System.Linq;

namespace Meezan.Application.Common;

/// <summary>
/// Result of calculating a single stock's sector weight.
/// </summary>
public record SectorStockWeightResult(
    int StockId,
    string Ticker,
    decimal? EffectiveMarketCapEgp,
    decimal? RawWeight,
    decimal? FinalWeight,
    bool IsCapped,
    decimal CappingFactor
);

/// <summary>
/// Result of calculating all sector weights for a given sector.
/// </summary>
public record SectorWeightsCalculationResult(
    int? SectorId,
    List<SectorStockWeightResult> StockWeights,
    decimal TotalWeightSum,
    bool HasSumWarning,
    string? WarningMessage
);

/// <summary>
/// Input item for sector weight calculation.
/// </summary>
public record SectorStockWeightInput(
    int StockId,
    string Ticker,
    decimal? MarketValue,
    string? Currency
);

/// <summary>
/// Computes capped market-cap weights for stocks within a sector.
/// Capped at 35% with iterative redistribution.
/// Foreign-currency stocks (USD) are converted to EGP.
/// Missing market caps receive the remainder if any, or null ("—") if total is already 100%.
/// </summary>
public static class SectorWeightCalculator
{
    public const decimal DefaultUsdExchangeRate = 52.40m;
    public const decimal MaxCapRatio = 0.35m; // 35%
    public const decimal MaxCapPercent = 35.0m;

    public static SectorWeightsCalculationResult Calculate(
        int? sectorId,
        IEnumerable<SectorStockWeightInput> stocks,
        decimal usdExchangeRate = DefaultUsdExchangeRate)
    {
        var stockList = stocks?.ToList() ?? new List<SectorStockWeightInput>();
        if (stockList.Count == 0)
        {
            return new SectorWeightsCalculationResult(sectorId, new List<SectorStockWeightResult>(), 0m, false, null);
        }

        // 1. Resolve Effective Market Cap in EGP
        // Currency can be "USD", "دولار", "$", etc.
        var validStocks = new List<(SectorStockWeightInput Input, decimal MarketCapEgp)>();
        var invalidStocks = new List<SectorStockWeightInput>();

        foreach (var s in stockList)
        {
            if (s.MarketValue.HasValue && s.MarketValue.Value > 0)
            {
                decimal capEgp = s.MarketValue.Value;
                bool isUsd = !string.IsNullOrWhiteSpace(s.Currency) &&
                    (s.Currency.Contains("USD", StringComparison.OrdinalIgnoreCase) ||
                     s.Currency.Contains("دولار", StringComparison.OrdinalIgnoreCase) ||
                     s.Currency.Contains("$"));

                if (isUsd)
                {
                    capEgp *= usdExchangeRate;
                }
                validStocks.Add((s, capEgp));
            }
            else
            {
                invalidStocks.Add(s);
            }
        }

        decimal totalMarketCapEgp = validStocks.Sum(x => x.MarketCapEgp);
        var results = new List<SectorStockWeightResult>();

        if (validStocks.Count == 0 || totalMarketCapEgp <= 0)
        {
            // All stocks lack valid market cap
            decimal equalWeight = invalidStocks.Count > 0 ? Math.Round(100m / invalidStocks.Count, 4) : 0m;
            foreach (var s in invalidStocks)
            {
                results.Add(new SectorStockWeightResult(
                    StockId: s.StockId,
                    Ticker: s.Ticker,
                    EffectiveMarketCapEgp: null,
                    RawWeight: null,
                    FinalWeight: equalWeight,
                    IsCapped: false,
                    CappingFactor: 1.0m
                ));
            }

            decimal sum = results.Sum(r => r.FinalWeight ?? 0m);
            return new SectorWeightsCalculationResult(sectorId, results, sum, false, null);
        }

        // 2. Compute Raw Weights
        // Raw weight = MarketCapEgp / TotalMarketCapEgp * 100
        var workingList = validStocks.Select(x => new
        {
            Input = x.Input,
            MarketCap = x.MarketCapEgp,
            RawWeight = Math.Round((x.MarketCapEgp / totalMarketCapEgp) * 100m, 6)
        }).ToList();

        // 3. Iterative 35% Capping
        // Algorithm from requirements:
        // Cap any single stock at 35%. If one or more stocks exceed 35%:
        // - uncapped total = sum of market cap of non-capped stocks
        // - new total = uncapped total / (1 - 0.35 * number of capped stocks)
        // - each capped stock's value = (new total - uncapped total) / number of capped stocks
        // - capping factor = capped value / stock's actual market cap (1 for all others)
        // - final weight = (market cap * capping factor) / sum of (market cap * capping factor)
        // - repeat until no stock exceeds 35%
        // NOTE: Capping at 35% only makes sense when the sector has >= 3 stocks or redistribution is possible,
        // because with <= 2 stocks, 2 * 35% = 70% leaving 30% unallocated.
        // If 1 - 0.35 * (numCapped + 1) <= 0 or nonCapped would have 0 stocks, capping stops.
        var cappedTickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pre-check if any stock exceeds 35% and capping is mathematically feasible
        if (workingList.Count >= 3)
        {
            while (true)
            {
                // Calculate weights with current capped set
                int numCapped = cappedTickers.Count;
                var nonCapped = workingList.Where(w => !cappedTickers.Contains(w.Input.Ticker)).ToList();

                if (nonCapped.Count == 0) break;

                decimal uncappedTotal = nonCapped.Sum(w => w.MarketCap);
                decimal denominator = 1.0m - (MaxCapRatio * numCapped);

                if (denominator <= 0 || uncappedTotal <= 0) break;

                decimal newTotal = uncappedTotal / denominator;

                // Check if adding another capped stock is mathematically possible:
                // denominator for numCapped + 1 must be > 0 (i.e. numCapped + 1 < 1 / 0.35 = 2.857 => max 2 capped stocks)
                // and nonCapped.Count must be > 1 so at least one non-capped stock remains
                if (1.0m - (MaxCapRatio * (numCapped + 1)) <= 0 || nonCapped.Count <= 1)
                {
                    break;
                }

                string? newlyCappedTicker = null;
                decimal highestExceeding = 0m;

                foreach (var nc in nonCapped)
                {
                    decimal weightPct = newTotal > 0 ? (nc.MarketCap / newTotal) * 100m : 0m;
                    if (weightPct > MaxCapPercent + 0.0001m)
                    {
                        if (weightPct > highestExceeding)
                        {
                            highestExceeding = weightPct;
                            newlyCappedTicker = nc.Input.Ticker;
                        }
                    }
                }

                if (newlyCappedTicker != null)
                {
                    cappedTickers.Add(newlyCappedTicker);
                    continue; // Repeat iteration
                }

                // No newly capped stocks found! We have converged.
                break;
            }
        }

        // Final values calculation
        int finalNumCapped = cappedTickers.Count;
        var finalNonCapped = workingList.Where(w => !cappedTickers.Contains(w.Input.Ticker)).ToList();
        decimal finalUncappedTotal = finalNonCapped.Sum(w => w.MarketCap);
        decimal finalDenom = 1.0m - (MaxCapRatio * finalNumCapped);
        decimal finalNewTotal = finalDenom > 0 ? finalUncappedTotal / finalDenom : finalUncappedTotal;
        decimal finalCappedStockValue = finalNumCapped > 0 && finalDenom > 0
            ? (finalNewTotal - finalUncappedTotal) / finalNumCapped
            : 0m;

        foreach (var item in workingList)
        {
            bool isCapped = cappedTickers.Contains(item.Input.Ticker);
            decimal cappingFactor = 1.0m;
            decimal finalWeight;

            if (isCapped)
            {
                cappingFactor = item.MarketCap > 0 ? finalCappedStockValue / item.MarketCap : 1.0m;
                finalWeight = MaxCapPercent;
            }
            else
            {
                cappingFactor = 1.0m;
                finalWeight = finalNewTotal > 0 ? (item.MarketCap / finalNewTotal) * 100m : 0m;
            }

            results.Add(new SectorStockWeightResult(
                StockId: item.Input.StockId,
                Ticker: item.Input.Ticker,
                EffectiveMarketCapEgp: Math.Round(item.MarketCap, 2),
                RawWeight: Math.Round(item.RawWeight, 4),
                FinalWeight: Math.Round(finalWeight, 4),
                IsCapped: isCapped,
                CappingFactor: Math.Round(cappingFactor, 6)
            ));
        }

        // 4. Handle invalid / missing market-cap stocks
        // "A stock without a valid القيمة السوقية stays in the sector (it still counts in the stock count
        // and is still displayed) and gets whatever percentage remains so the sector total is exactly 100%.
        // If nothing remains (the other stocks already sum to 100%), show its weight as '—' instead of a number,
        // and never let it affect the other stocks' weights."
        decimal currentSum = results.Sum(r => r.FinalWeight ?? 0m);
        decimal remainder = Math.Max(0m, 100m - currentSum);

        decimal? weightForInvalid = null;
        if (remainder > 0.001m && invalidStocks.Count > 0)
        {
            weightForInvalid = Math.Round(remainder / invalidStocks.Count, 4);
        }

        foreach (var s in invalidStocks)
        {
            results.Add(new SectorStockWeightResult(
                StockId: s.StockId,
                Ticker: s.Ticker,
                EffectiveMarketCapEgp: null,
                RawWeight: null,
                FinalWeight: weightForInvalid, // null if nothing remains
                IsCapped: false,
                CappingFactor: 1.0m
            ));
        }

        // 5. Automatic check that warns if weights do not sum to 100%
        decimal totalSum = results.Sum(r => r.FinalWeight ?? 0m);
        bool hasWarning = Math.Abs(totalSum - 100m) > 0.1m;
        string? warningMessage = hasWarning
            ? $"تنبيه: مجموع أوزان أسهم القطاع ({totalSum:F2}%) لا يساوي 100%."
            : null;

        return new SectorWeightsCalculationResult(
            SectorId: sectorId,
            StockWeights: results,
            TotalWeightSum: Math.Round(totalSum, 4),
            HasSumWarning: hasWarning,
            WarningMessage: warningMessage
        );
    }
}
