namespace Meezan.Application.Common.Models;

public class ExternalStockMergedDto
{
    public string? Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Market { get; set; }
    public DateTime? LastUpdated { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Currency { get; set; }
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
    public string? Category { get; set; }
    public string? CategoryAr { get; set; }

    public ExternalSourceOpinionDto? HalalBourse { get; set; }
    public ExternalSourceOpinionDto? Musaffa { get; set; }
    public ExternalSourceOpinionDto? Kashif { get; set; }
    public ExternalSourceOpinionDto? HalalInvest { get; set; }
    public ExternalSourceOpinionDto? FaisalBank { get; set; }
    public ExternalSourceOpinionDto? Osoul { get; set; }
    public ExternalSourceOpinionDto? Thndr { get; set; }
}

public class ExternalSourceOpinionDto
{
    public string? Status { get; set; }
    public decimal? Percentage { get; set; }
    public string? Note { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? ExtraDataJson { get; set; }
}
