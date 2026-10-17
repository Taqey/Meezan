namespace Meezan.Domain.Entities;

/// <summary>
/// Top-level numeric and flag fields from the external stocks_merged.json — one row per stock.
/// </summary>
public class StockShariahMetrics
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal? Zakat { get; set; }
    public decimal? SpHaramEarningPercentage { get; set; }
    public decimal? AaoifiHaramEarningPerShare { get; set; }
    public decimal? HaramEarningsPercentage { get; set; }
    public decimal? LoansPercentage { get; set; }
    public decimal? FairValueValuation { get; set; }
    public decimal? BookValue { get; set; }
    public decimal? Profit { get; set; }
    public decimal? Dividend { get; set; }
    public string? DividendType { get; set; }
    public bool? CoreActivityCompliant { get; set; }
    public bool? CashLiquidityCompliant { get; set; }
    public bool? HaramInvestmentsCompliant { get; set; }
    public string? CategoryEn { get; set; }
    public string? CategoryAr { get; set; }
    public DateTime? SourceUpdatedAt { get; set; }
    public DateTime FetchedAt { get; set; }

    // ── Manual overrides (admin only) ──────────────────────────────────────────
    /// <summary>
    /// Manual override for core activity compliance flag. Null = use feed value.
    /// When true, the stock's activity is considered compliant regardless of feed.
    /// </summary>
    public bool? CoreActivityCompliantOverride { get; set; }

    /// <summary>
    /// Manual override for activity classification (Arabic). Null = use feed value.
    /// </summary>
    public string? CategoryArOverride { get; set; }

    /// <summary>
    /// Manual override for activity classification (English). Null = use feed value.
    /// </summary>
    public string? CategoryEnOverride { get; set; }

    /// <summary>
    /// Manual override for haram revenue percentage (0-100). Null = use feed value.
    /// </summary>
    public decimal? HaramRevenuePercentageOverride { get; set; }

    /// <summary>
    /// Manual override for loans/interest percentage (0-100). Null = use feed value.
    /// </summary>
    public decimal? LoansPercentageOverride { get; set; }

    public Stock? Stock { get; set; }

    // ── Effective value helpers (used by readers) ──────────────────────────────
    /// <summary>Effective core activity compliant: override wins over feed.</summary>
    public bool? EffectiveCoreActivityCompliant => CoreActivityCompliantOverride ?? CoreActivityCompliant;

    /// <summary>Effective activity classification (Arabic): override wins over feed.</summary>
    public string? EffectiveCategoryAr => CategoryArOverride ?? CategoryAr;

    /// <summary>Effective activity classification (English): override wins over feed.</summary>
    public string? EffectiveCategoryEn => CategoryEnOverride ?? CategoryEn;

    /// <summary>Effective haram revenue percentage: override wins over feed.</summary>
    public decimal? EffectiveHaramRevenuePercentage => HaramRevenuePercentageOverride ?? HaramEarningsPercentage;

    /// <summary>Effective loans/interest percentage: override wins over feed.</summary>
    public decimal? EffectiveLoansPercentage => LoansPercentageOverride ?? LoansPercentage;

    /// <summary>True if any manual override is set.</summary>
    public bool HasAnyOverride =>
        CoreActivityCompliantOverride.HasValue
        || !string.IsNullOrWhiteSpace(CategoryArOverride)
        || !string.IsNullOrWhiteSpace(CategoryEnOverride)
        || HaramRevenuePercentageOverride.HasValue
        || LoansPercentageOverride.HasValue;
}
