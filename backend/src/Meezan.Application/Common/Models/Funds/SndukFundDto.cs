using System.Text.Json.Serialization;

namespace Meezan.Application.Common.Models.Funds;

public sealed class SndukFundDto
{
    public long Id { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string PublicId { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Slug { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CustomSlug { get; set; }
    public string Name { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? NameEn { get; set; }
    public string Code { get; set; } = string.Empty;
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Type { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TypeNameAr { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TypeNameEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? RiskLevel { get; set; }
    public bool? IsIslamic { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsActive { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CurrentPrice { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? DailyChange { get; set; }
    public SndukReturnsDto? Returns { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? TotalScore { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Aum { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ManagementFee { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? MinimumInvestment { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Currency { get; set; }
    [JsonConverter(typeof(FlexibleLongConverter))]
    public long? AssetManagerId { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerName { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerNameEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? AssetManagerCode { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? LogoUrl { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Description { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? DescriptionEn { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? InvestmentStrategy { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? InceptionDate { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? ProspectusUrl { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? CreatedAt { get; set; }
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? UpdatedAt { get; set; }
}
