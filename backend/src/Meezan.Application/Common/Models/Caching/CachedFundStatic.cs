using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Common.Models.Caching;

public sealed class CachedFundStatic
{
    public int Id { get; set; }
    public long SndukId { get; set; }
    public string PublicId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public FundType Type { get; set; }
    public string TypeNameAr { get; set; } = string.Empty;
    public string TypeNameEn { get; set; } = string.Empty;
    public FundRiskLevel RiskLevel { get; set; }
    public bool IsIslamic { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? Aum { get; set; }
    public decimal? ManagementFee { get; set; }
    public string MinimumInvestment { get; set; } = string.Empty;
    public string Currency { get; set; } = "EGP";
    public long? AssetManagerId { get; set; }
    public string AssetManagerName { get; set; } = string.Empty;
    public string AssetManagerNameEn { get; set; } = string.Empty;
    public string AssetManagerCode { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string LocalLogoPath { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    public string InvestmentStrategy { get; set; } = string.Empty;
    public DateTime? InceptionDate { get; set; }
    public string ProspectusUrl { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }

    public EffectiveClassificationDto? EffectiveClassification { get; set; }
    public CachedFundAnalysis? Analysis { get; set; }
    public List<int> SimilarFundIds { get; set; } = new();
    public List<int> CategoryIds { get; set; } = new();
}
