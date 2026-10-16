namespace Meezan.WebApi.Contracts.AdminFunds;

public sealed class UpdateCategoryRequest
{
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Slug { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public bool? IsIndexTracking { get; set; }
    public bool? IsVisible { get; set; }
    public int? DisplayOrder { get; set; }
    public string? AutoRuleJson { get; set; }
}
