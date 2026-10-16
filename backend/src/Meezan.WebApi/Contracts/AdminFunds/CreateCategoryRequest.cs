namespace Meezan.WebApi.Contracts.AdminFunds;

public sealed class CreateCategoryRequest
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool IsAutoDerived { get; set; }
    public bool IsIndexTracking { get; set; }
    public string? AutoRuleJson { get; set; }
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}
