namespace Meezan.Application.Features.Funds.Classification.Dtos;

public sealed class FundClassificationTreeDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? ParentSlug { get; set; }
    public int DisplayOrder { get; set; }
    public string ChildrenLabelAr { get; set; } = string.Empty;
    public string ChildrenLabelEn { get; set; } = string.Empty;
    public int DirectFundCount { get; set; }
    public int TotalFundCount { get; set; }
    public bool HasAutoRule { get; set; }
}
