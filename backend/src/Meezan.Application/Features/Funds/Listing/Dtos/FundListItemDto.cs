using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Listing.Dtos;

public sealed class FundListItemDto : IFundTypeFields
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
    /// <summary>Id of the editable type row backing the effective type.</summary>
    public int? TypeId { get; set; }
    /// <summary>Stable slug of the effective type (e.g. equity).</summary>
    public string TypeSlug { get; set; } = string.Empty;
    /// <summary>True when an operator manually overrode the SNDUK-derived type.</summary>
    public bool TypeIsOverridden { get; set; }
    public string ClassificationSlug { get; set; } = string.Empty;
    public string ClassificationPathAr { get; set; } = string.Empty;
    public string ClassificationSource { get; set; } = string.Empty;
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
    public string LogoUrl { get; set; } = string.Empty;
    public string LocalLogoPath { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
}
