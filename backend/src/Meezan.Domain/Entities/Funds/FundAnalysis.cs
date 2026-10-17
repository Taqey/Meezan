namespace Meezan.Domain.Entities;

/// <summary>
/// Fund price history point for charts.
/// </summary>
public class FundPriceHistory
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? Change { get; set; }
    public decimal? Performance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
}

/// <summary>
/// Fund analysis from SNDUK (scores and metrics).
/// </summary>
public class FundAnalysis
{
    public int Id { get; set; }
    public int FundId { get; set; }

    // Scores (-1 means not available)
    public decimal? PerformanceScore { get; set; }
    public decimal? RiskScore { get; set; }
    public decimal? CostScore { get; set; }
    public decimal? StabilityScore { get; set; }
    public decimal? DiversificationScore { get; set; }
    public decimal? TotalScore { get; set; }

    // Raw metrics
    public decimal? Cagr { get; set; }
    public decimal? Alpha { get; set; }
    public decimal? Volatility { get; set; }
    public decimal? Consistency { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public decimal? SharpeRatio { get; set; }

    // Weights (if available)
    public string WeightsJson { get; set; } = string.Empty;

    // Explanations
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
}

/// <summary>
/// Similar/Competing funds from SNDUK.
/// </summary>
public class FundSimilar
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public int SimilarFundId { get; set; } // references Fund.Id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
    public Fund? SimilarFund { get; set; }
}