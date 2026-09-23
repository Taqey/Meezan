using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Scraping.Services;

/// <summary>
/// Pure, unit-testable fair-value calculation service.
/// No HTTP calls or parsing — only math on already-scraped values.
/// </summary>
public static class FairValueCalculator
{
    // IQR outlier detection uses Tukey fences:
    //   LowerBound = Q1 − IqrMultiplier × IQR
    //   UpperBound = Q3 + IqrMultiplier × IQR
    // 1.5 is the universally accepted standard (John Tukey, 1977).
    private const double IqrMultiplier = 1.5;

    // Price must differ by more than this fraction from fair value to be
    // classified as Cheap or Expensive.
    private const double PriceTolerancePct = 0.05; // ±5%

    // Caps for "valid" sector-peer ratios used in median computation.
    public const double MaxValidPeRatio = 100.0;
    public const double MaxValidPbRatio = 20.0;

    /// <summary>
    /// Computes per-sector median PE and PB from a batch of scraped snapshots.
    /// Each stock is excluded from its own sector's median (peer-only median).
    /// Only ratios that are positive and below the validity caps are included.
    /// Returns a dictionary keyed by SectorId.
    /// </summary>
    public static Dictionary<int, SectorMedians> ComputeSectorMedians(
        IReadOnlyList<SectorPeerData> peerData)
    {
        var result = new Dictionary<int, SectorMedians>();

        // Group by sector
        var bySector = peerData
            .Where(p => p.SectorId.HasValue)
            .GroupBy(p => p.SectorId!.Value)
            .ToList();

        foreach (var sectorGroup in bySector)
        {
            int sectorId = sectorGroup.Key;
            var peers = sectorGroup.ToList();

            // For each stock, compute the median from all OTHER peers in its sector.
            // Since the result is per-sector (not per-stock), we simply compute one
            // median for the entire group; callers must exclude the stock itself.
            // We store the full list so the handler can exclude-self at call time.
            result[sectorId] = new SectorMedians(
                PeValues: peers
                    .Where(p => p.PeRatio.HasValue
                                && p.PeRatio.Value > 0
                                && (double)p.PeRatio.Value <= MaxValidPeRatio)
                    .Select(p => (double)p.PeRatio!.Value)
                    .ToList(),
                PbValues: peers
                    .Where(p => p.PbRatio.HasValue
                                && p.PbRatio.Value > 0
                                && (double)p.PbRatio.Value <= MaxValidPbRatio)
                    .Select(p => (double)p.PbRatio!.Value)
                    .ToList(),
                Peers: peers
            );
        }

        return result;
    }

    /// <summary>
    /// Builds the list of named estimates from the scraped market data,
    /// using sector-peer medians for the PE- and PB-based methods instead
    /// of the stock's own ratios (which would be circular).
    ///
    /// Methods:
    ///   1. Graham Number      = √(22.5 × EPS × BookValue)
    ///   2. SectorMedianPE×EPS = SectorMedianPE  × this stock's EPS
    ///   3. SectorMedianPB×BV  = SectorMedianPB  × this stock's BookValue
    ///   4. Direct Book Value  = this stock's BookValue
    /// </summary>
    public static List<(string Name, decimal Value)> BuildMethodEstimates(
        ScrapedMarketDataValues v,
        SectorMedians? sectorMedians,
        int? stockSectorId)
    {
        var estimates = new List<(string Name, decimal Value)>();

        // Method 1: Graham Number = √(22.5 × EPS × BookValue)
        if (v.Eps.HasValue && v.BookValue.HasValue
            && v.Eps.Value > 0 && v.BookValue.Value > 0)
        {
            double grahamSq = 22.5 * (double)v.Eps.Value * (double)v.BookValue.Value;
            if (grahamSq > 0)
                estimates.Add(("Graham", (decimal)Math.Sqrt(grahamSq)));
        }

        if (sectorMedians is not null && stockSectorId.HasValue)
        {
            // Method 2: Sector-Median PE × this stock's EPS
            // Exclude this stock's own PE from the peer list before computing median.
            var medianPe = ComputeMedianExcludingSelf(
                sectorMedians.PeValues,
                v.PeRatio.HasValue && v.PeRatio.Value > 0 && (double)v.PeRatio.Value <= MaxValidPeRatio
                    ? (double?)v.PeRatio.Value
                    : null);

            if (medianPe.HasValue && v.Eps.HasValue && v.Eps.Value > 0)
                estimates.Add(("SectorPE×EPS", (decimal)medianPe.Value * v.Eps.Value));

            // Method 3: Sector-Median PB × this stock's BookValue
            var medianPb = ComputeMedianExcludingSelf(
                sectorMedians.PbValues,
                v.PbRatio.HasValue && v.PbRatio.Value > 0 && (double)v.PbRatio.Value <= MaxValidPbRatio
                    ? (double?)v.PbRatio.Value
                    : null);

            if (medianPb.HasValue && v.BookValue.HasValue && v.BookValue.Value > 0)
                estimates.Add(("SectorPB×BV", (decimal)medianPb.Value * v.BookValue.Value));
        }

        // Method 4: Direct Book Value (always included if available and positive)
        if (v.BookValue.HasValue && v.BookValue.Value > 0)
            estimates.Add(("BookValue", v.BookValue.Value));

        return estimates;
    }

    /// <summary>
    /// Computes the fair value by averaging valid methods, excluding outliers
    /// via the IQR (Interquartile Range) method — Tukey fences with multiplier 1.5.
    /// Q1 and Q3 use linear interpolation between adjacent ranks (identical to
    /// Excel QUARTILE.INC and NumPy percentile default), which handles small n
    /// (3–4 estimates) consistently without special-casing.
    /// Returns Empty if no estimates are provided.
    /// </summary>
    public static FairValueResult Compute(
        List<(string Name, decimal Value)> estimates,
        decimal? currentPrice)
    {
        if (estimates.Count == 0)
            return FairValueResult.Empty();

        // 1. If only one estimate exists, IQR is undefined — skip outlier detection.
        List<(string Name, decimal Value)> included;
        List<(string Name, decimal Value)> excluded;

        if (estimates.Count == 1)
        {
            included = estimates;
            excluded = [];
        }
        else
        {
            // 2. IQR-based outlier detection (Tukey fences).
            //
            // Q1/Q3 use linear interpolation ("exclusive" / type-7 method):
            //   index = p × (n − 1)  where p = 0.25 or 0.75
            //   Q = sorted[floor(index)] + frac(index) × (sorted[ceil(index)] − sorted[floor(index)])
            //
            // This is the same formula used by Excel QUARTILE.INC, NumPy (default),
            // R (type 7), and Pandas. It is well-defined for any n ≥ 2 and gives
            // sensible results with 3–4 points without any special-casing.
            //
            // Note: at very small n (3–4), the IQR bounds can be wide enough that
            // few or no values get excluded — this is expected statistical behaviour,
            // not a bug. The method is applied consistently at all n.
            var (lower, upper) = ComputeIqrBounds(estimates.Select(e => (double)e.Value).ToList());

            included = estimates
                .Where(e => (double)e.Value >= lower && (double)e.Value <= upper)
                .ToList();

            excluded = estimates.Except(included).ToList();

            if (included.Count == 0)
            {
                // All estimates fell outside the IQR bounds (can happen at very small n).
                // Fall back to using all of them so we always produce a result.
                included = estimates;
                excluded = [];
            }
        }

        var fairValue = (decimal)included.Select(e => (double)e.Value).Average();

        // 3. Price comparison
        var comparison = PriceComparison.Fair;
        decimal? diffAbs = null;
        decimal? diffPct = null;

        if (currentPrice.HasValue && currentPrice.Value > 0 && fairValue > 0)
        {
            // Diff = ClosingPrice - FairValue (matching scraper.py compute_fair_value_diff)
            // Negative -> current price is below fair value (cheap)
            // Positive -> current price is above fair value (expensive)
            diffAbs = currentPrice.Value - fairValue;
            diffPct = (diffAbs / currentPrice.Value) * 100m;

            // Comparison logic matching scraper.py compute_price_comparison:
            // FairValue > ClosingPrice * 1.05 -> Cheap ("أصغر")
            // FairValue < ClosingPrice * 0.95 -> Expensive ("أكبر")
            // Otherwise -> Fair ("تقريبًا قدها")
            if (fairValue > currentPrice.Value * 1.05m)
                comparison = PriceComparison.Cheap;
            else if (fairValue < currentPrice.Value * 0.95m)
                comparison = PriceComparison.Expensive;
            else
                comparison = PriceComparison.Fair;
        }

        // 4. Confidence: based on number of non-outlier methods used
        var confidence = included.Count switch
        {
            >= 3 => ValuationConfidence.High,
            2    => ValuationConfidence.Medium,
            1    => ValuationConfidence.Low,
            _    => ValuationConfidence.None
        };

        var methodResults = estimates.Select(e => new FairValueMethodResult(
            e.Name,
            e.Value,
            IsOutlier: excluded.Any(ex => ex.Name == e.Name)
        )).ToList();

        return new FairValueResult(
            FairValue: fairValue,
            Comparison: comparison,
            DiffAbs: diffAbs,
            DiffPct: diffPct,
            MethodsUsedCount: included.Count,
            MethodsExcludedCount: excluded.Count,
            Confidence: confidence,
            Methods: methodResults
        );
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Computes Tukey IQR fence bounds for a set of values.
    ///
    /// Percentile method: linear interpolation between adjacent sorted ranks
    /// (the "type 7" / "inclusive" method):
    ///   index = p × (n − 1),  0-based into the sorted array
    ///   Q     = sorted[⌊index⌋] + frac(index) × (sorted[⌈index⌉] − sorted[⌊index⌋])
    ///
    /// This is identical to Excel QUARTILE.INC, NumPy default (np.percentile),
    /// R type 7, and Pandas default. Works for n ≥ 2 without special-casing.
    ///
    /// Fences:
    ///   LowerBound = Q1 − 1.5 × IQR
    ///   UpperBound = Q3 + 1.5 × IQR
    /// </summary>
    private static (double Lower, double Upper) ComputeIqrBounds(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int n = sorted.Count;

        static double Percentile(List<double> s, double p)
        {
            double idx = p * (s.Count - 1);
            int lo = (int)Math.Floor(idx);
            int hi = (int)Math.Ceiling(idx);
            double frac = idx - lo;
            return lo == hi ? s[lo] : s[lo] + frac * (s[hi] - s[lo]);
        }

        double q1  = Percentile(sorted, 0.25);
        double q3  = Percentile(sorted, 0.75);
        double iqr = q3 - q1;

        return (
            Lower: q1 - IqrMultiplier * iqr,
            Upper: q3 + IqrMultiplier * iqr
        );
    }

    /// <summary>
    /// Returns the median of a list of values after optionally removing one
    /// occurrence of selfValue (the current stock's own ratio).
    /// Returns null if fewer than 1 valid peer remains.
    /// </summary>

    private static double? ComputeMedianExcludingSelf(
        IReadOnlyList<double> allValues,
        double? selfValue)
    {
        var values = allValues.ToList();

        // Remove one occurrence of this stock's own ratio so the peer median
        // is truly from *other* stocks only.
        if (selfValue.HasValue)
        {
            int idx = values.FindIndex(v => Math.Abs(v - selfValue.Value) < 1e-9);
            if (idx >= 0) values.RemoveAt(idx);
        }

        if (values.Count == 0) return null;

        values.Sort();
        int mid = values.Count / 2;
        return values.Count % 2 == 1
            ? values[mid]
            : (values[mid - 1] + values[mid]) / 2.0;
    }

    // ── Data-transfer types ───────────────────────────────────────────────────

    /// <summary>
    /// Per-sector aggregated ratio lists (raw values, not yet a median).
    /// Computed once per scrape run; passed into BuildMethodEstimates per-stock.
    /// </summary>
    public record SectorMedians(
        List<double> PeValues,
        List<double> PbValues,
        List<SectorPeerData> Peers
    );

    /// <summary>
    /// Lightweight projection of a scraped stock used for sector-median computation.
    /// </summary>
    public record SectorPeerData(
        int StockId,
        int? SectorId,
        decimal? PeRatio,
        decimal? PbRatio
    );

    public record ScrapedMarketDataValues(
        decimal? Eps,
        decimal? PeRatio,
        decimal? BookValue,
        decimal? PbRatio
    );

    public record FairValueMethodResult(
        string Name,
        decimal Value,
        bool IsOutlier
    );

    public record FairValueResult(
        decimal? FairValue,
        PriceComparison Comparison,
        decimal? DiffAbs,
        decimal? DiffPct,
        int MethodsUsedCount,
        int MethodsExcludedCount,
        ValuationConfidence Confidence,
        List<FairValueMethodResult> Methods
    )
    {
        public static FairValueResult Empty() => new(
            FairValue: null,
            Comparison: PriceComparison.Fair,
            DiffAbs: null,
            DiffPct: null,
            MethodsUsedCount: 0,
            MethodsExcludedCount: 0,
            Confidence: ValuationConfidence.None,
            Methods: []
        );
    }
}
