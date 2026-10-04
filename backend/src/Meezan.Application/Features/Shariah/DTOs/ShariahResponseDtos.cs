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
    /// <summary>
    /// AAOIFI screen: null (unknown) when both inputs are missing; otherwise a present
    /// value fails the screen when it breaches its threshold (any haram per-share &gt; 0,
    /// loans &gt;= 30%). Missing inputs never invent a failure — they are treated as pass
    /// for the present-value check, but both-missing stays null so the UI can show
    /// "unavailable" instead of a fabricated pass.
    /// </summary>
    public bool? IsCompliantAaoifi
    {
        get
        {
            if (AaoifiHaramEarningPerShare == null && LoansPercentage == null)
                return null;
            var aaoifiOk = AaoifiHaramEarningPerShare == null || AaoifiHaramEarningPerShare == 0;
            var loansOk = LoansPercentage == null || LoansPercentage < 30;
            return aaoifiOk && loansOk;
        }
    }
    /// <summary>
    /// S&amp;P screen: null (unknown) when both inputs are missing; otherwise a present
    /// value fails when it breaches its threshold (non-compliant income &gt;= 5%,
    /// loans &gt;= 30%).
    /// </summary>
    public bool? IsCompliantSp
    {
        get
        {
            if (SpHaramEarningPercentage == null && LoansPercentage == null)
                return null;
            var spOk = SpHaramEarningPercentage == null || SpHaramEarningPercentage < 5;
            var loansOk = LoansPercentage == null || LoansPercentage < 30;
            return spOk && loansOk;
        }
    }
    public string? ActivityClassification => CategoryAr ?? CategoryEn;
}

public class FullStockShariahDto
{
    public ShariahComplianceDto? Compliance { get; set; }
    public StockShariahMetricsDto? Metrics { get; set; }
    public List<ShariahSourceOpinionDto> Opinions { get; set; } = new();
}
