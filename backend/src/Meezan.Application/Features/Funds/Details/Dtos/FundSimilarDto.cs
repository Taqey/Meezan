namespace Meezan.Application.Features.Funds.Details.Dtos;

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
