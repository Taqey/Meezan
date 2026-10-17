namespace Meezan.Application.Features.Funds.Classification.Dtos;

public sealed class EffectiveClassificationDto
{
    public int? NodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RootSlug { get; set; } = string.Empty;
    public string RootNameAr { get; set; } = string.Empty;
    public string RootNameEn { get; set; } = string.Empty;
    public string LeafSlug { get; set; } = string.Empty;
    public string LeafNameAr { get; set; } = string.Empty;
    public string LeafNameEn { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> PathSlugs { get; set; } = new();
    public List<string> PathNamesAr { get; set; } = new();
    public List<string> PathNamesEn { get; set; } = new();
}
