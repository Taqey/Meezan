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

    public Stock? Stock { get; set; }
}
