namespace Meezan.WebApi.Contracts.Classification;

public sealed class CreateNodeRequest
{
    public int? ParentId { get; set; }
    public string? Slug { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsVisible { get; set; }
    public bool? RequiresChildSelection { get; set; }
    public string? ChildrenLabelAr { get; set; }
    public string? ChildrenLabelEn { get; set; }
    public string? AutoRuleJson { get; set; }
    public bool? AutoClassificationEnabled { get; set; }
    public string? FundDisposition { get; set; }
}
