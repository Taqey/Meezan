using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.DTOs;

public class ShariahComplianceDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string SymbolCode { get; set; } = string.Empty;
    public string? StockNameAr { get; set; }
    public string? StockNameEn { get; set; }
    public ShariahStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public decimal? Pct { get; set; }
    public string? Note { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ShariahSourceOpinionDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public ShariahSourceKey SourceKey { get; set; }
    public string SourceKeyName => SourceKey.ToString();
    /// <summary>
    /// This board's own verdict for THIS stock, exactly as stored — nothing is invented.
    /// Three-state rule for all 7 boards (FaisalBank, Osoul, HalalBourse, Musaffa, Thndr,
    /// Kashif, HalalInvest), evaluated per (stock, board) pair: listed as compliant →
    /// "compliant"; listed as non-compliant → "non_compliant"; stock not covered by that
    /// board → null (no row / no verdict) = "لا يوجد رأي". Callers must surface null as
    /// "لا يوجد رأي", must never substitute a متوافق / غير متوافق verdict, and must leave
    /// it out of any compliance aggregate (denominator = boards with a recorded opinion).
    /// </summary>
    public string? Status { get; set; }
    public decimal? Percentage { get; set; }
    public string? Note { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? SourceLastUpdated { get; set; }
    public DateTime FetchedAt { get; set; }
    public string? ExtraData { get; set; }
}

public class StockShariahMetricsDto
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

    // Extended/computed properties for UI
    public decimal? InterestBearingDebtRatio => LoansPercentage;
    public bool? IsCompliantActivity => CoreActivityCompliant;
    public bool? IsCompliantAaoifi => (AaoifiHaramEarningPerShare == null || AaoifiHaramEarningPerShare == 0) && (LoansPercentage == null || LoansPercentage < 30);
    public bool? IsCompliantSp => (SpHaramEarningPercentage == null || SpHaramEarningPercentage < 5) && (LoansPercentage == null || LoansPercentage < 30);
    public string? ActivityClassification => CategoryAr ?? CategoryEn;
}

public class FullStockShariahDto
{
    public ShariahComplianceDto? Compliance { get; set; }
    public StockShariahMetricsDto? Metrics { get; set; }
    public List<ShariahSourceOpinionDto> Opinions { get; set; } = new();
}
