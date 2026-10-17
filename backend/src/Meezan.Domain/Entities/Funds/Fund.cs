using Meezan.Domain.Enums;

namespace Meezan.Domain.Entities;

/// <summary>
/// Egyptian investment fund from SNDUK.
/// </summary>
public class Fund
{
    public int Id { get; set; }
    public long SndukId { get; set; } // numeric id from SNDUK
    public string PublicId { get; set; } = string.Empty; // publicId from SNDUK
    public string Slug { get; set; } = string.Empty; // slug or customSlug
    
    // Names
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    
    // Type & classification
    public FundType Type { get; set; }
    public string TypeNameAr { get; set; } = string.Empty;
    public string TypeNameEn { get; set; } = string.Empty;
    public FundRiskLevel RiskLevel { get; set; }
    public bool IsIslamic { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    
    // Pricing & returns
    public decimal? CurrentPrice { get; set; }
    public decimal? DailyChange { get; set; }
    public decimal? Return1M { get; set; }
    public decimal? Return3M { get; set; }
    public decimal? Return6M { get; set; }
    public decimal? Return1Y { get; set; }
    public decimal? Return3Y { get; set; }
    public decimal? TotalScore { get; set; }
    
    // Fund details
    public decimal? Aum { get; set; } // Assets under management
    public decimal? ManagementFee { get; set; }
    public string MinimumInvestment { get; set; } = string.Empty; // free text
    public string Currency { get; set; } = "EGP";
    
    // Asset manager
    public long? AssetManagerId { get; set; }
    public string AssetManagerName { get; set; } = string.Empty;
    public string AssetManagerNameEn { get; set; } = string.Empty;
    public string AssetManagerCode { get; set; } = string.Empty;
    
    // Media & description
    public string LogoUrl { get; set; } = string.Empty;
    public string LocalLogoPath { get; set; } = string.Empty; // downloaded logo
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string InvestmentStrategy { get; set; } = string.Empty;
    public DateTime? InceptionDate { get; set; }
    public string ProspectusUrl { get; set; } = string.Empty;
    
    // Timestamps
    public DateTime SndukCreatedAt { get; set; }
    public DateTime SndukUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedAt { get; set; }
    
    // Navigation
    public ICollection<FundPriceHistory> PriceHistory { get; set; } = new List<FundPriceHistory>();
    public FundAnalysis? Analysis { get; set; }
    public ICollection<FundSimilar> SimilarFunds { get; set; } = new List<FundSimilar>();
    public ICollection<FundCategoryOverride> CategoryOverrides { get; set; } = new List<FundCategoryOverride>();
}

/// <summary>
/// Fund type enum matching SNDUK types.
/// </summary>
public enum FundType
{
    Unknown = 0,
    Balanced = 1,
    CapitalPreservation = 2,
    Commodities = 3,
    Equity = 4,
    FixedIncome = 5,
    FixedIncomeUsd = 6,
    Gold = 7,
    Index = 8,
    MoneyMarket = 9
}

/// <summary>
/// Fund risk level.
/// </summary>
public enum FundRiskLevel
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3
}