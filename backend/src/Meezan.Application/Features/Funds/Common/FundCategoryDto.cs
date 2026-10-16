namespace Meezan.Application.Features.Funds.Common;

public sealed class FundCategoryDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsAutoDerived { get; set; }
    public int DisplayOrder { get; set; }
}
