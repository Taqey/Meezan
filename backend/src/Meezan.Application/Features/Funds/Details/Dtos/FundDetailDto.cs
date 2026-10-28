using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Details.Dtos;

public sealed class FundDetailDto : IFundTypeFields
{
    // Base fund info
    public int Id { get; set; }
    public long SndukId { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsNameManuallyEdited { get; set; }
    public bool IsDescriptionManuallyEdited { get; set; }
    public string Code { get; set; } = string.Empty;
    public FundType Type { get; set; }
    public string TypeNameAr { get; set; } = string.Empty;
    public string TypeNameEn { get; set; } = string.Empty;
    /// <summary>Id of the editable type row backing the effective type.</summary>
    public int? TypeId { get; set; }
    /// <summary>Stable slug of the effective type (e.g. equity).</summary>
    public string TypeSlug { get; set; } = string.Empty;
    /// <summary>True when an operator manually overrode the SNDUK-derived type.</summary>
    public bool TypeIsOverridden { get; set; }
    public FundRiskLevel RiskLevel { get; set; }
    public bool IsIslamic { get; set; }
    public bool IsFeatured { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? DailyChange { get; set; }
    public decimal? Return1M { get; set; }
    public decimal? Return3M { get; set; }
    public decimal? Return6M { get; set; }
    public decimal? Return1Y { get; set; }
    public decimal? Return3Y { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? Aum { get; set; }
    public decimal? ManagementFee { get; set; }
    public string MinimumInvestment { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public long? AssetManagerId { get; set; }
    public string AssetManagerName { get; set; } = string.Empty;
    public string AssetManagerNameEn { get; set; } = string.Empty;
    public string AssetManagerCode { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string LocalLogoPath { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
    public string ClassificationSlug { get; set; } = string.Empty;
    public string ClassificationPathAr { get; set; } = string.Empty;
    public string ClassificationSource { get; set; } = string.Empty;
    public Meezan.Application.Features.Funds.Classification.Dtos.EffectiveClassificationDto? EffectiveClassification { get; set; }

    // Detail-specific fields
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string InvestmentStrategy { get; set; } = string.Empty;
    public DateTime? InceptionDate { get; set; }
    public string ProspectusUrl { get; set; } = string.Empty;
    public List<FundPricePointDto> PriceHistory { get; set; } = [];
    public FundAnalysisDto? Analysis { get; set; }
    public List<FundSimilarDto> SimilarFunds { get; set; } = [];
    public List<FundCategoryMembershipDto> CategoryMemberships { get; set; } = [];
}

public sealed class FundPricePointDto
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? Change { get; set; }
    public decimal? Performance { get; set; }
}

public sealed class FundAnalysisDto
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

public sealed class FundSimilarDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    /// <summary>Local logo path of the similar fund, when one was downloaded.</summary>
    public string LocalLogoPath { get; set; } = string.Empty;
    /// <summary>Original SNDUK logo URL, used when no local file exists.</summary>
    public string LogoUrl { get; set; } = string.Empty;
    public string TypeNameAr { get; set; } = string.Empty;
    public string TypeNameEn { get; set; } = string.Empty;
    public string TypeSlug { get; set; } = string.Empty;
    public string ClassificationPathAr { get; set; } = string.Empty;
    public Meezan.Application.Features.Funds.Classification.Dtos.EffectiveClassificationDto? EffectiveClassification { get; set; }
}

public sealed class FundCategoryMembershipDto
{
    public int CategoryId { get; set; }
    public string CategoryNameAr { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
