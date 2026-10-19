namespace Meezan.Application.Common.Models.Caching;

public sealed class CachedFundAnalysis
{
    public decimal? PerformanceScore { get; set; }
    public decimal? RiskScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? StabilityScore { get; set; }
    public decimal? DiversificationScore { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? Cagr { get; set; }
    public decimal? Alpha { get; set; }
    public decimal? Volatility { get; set; }
    public decimal? Consistency { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? SharpeRatio { get; set; }
    public string WeightsJson { get; set; } = string.Empty;
    public string PerformanceExplanationAr { get; set; } = string.Empty;
    public string PerformanceExplanationEn { get; set; } = string.Empty;
    public string RiskExplanationAr { get; set; } = string.Empty;
    public string RiskExplanationEn { get; set; } = string.Empty;
    public string CostExplanationAr { get; set; } = string.Empty;
    public string CostExplanationEn { get; set; } = string.Empty;
    public string StabilityExplanationAr { get; set; } = string.Empty;
    public string StabilityExplanationEn { get; set; } = string.Empty;
    public string DiversificationExplanationAr { get; set; } = string.Empty;
    public string DiversificationExplanationEn { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}
