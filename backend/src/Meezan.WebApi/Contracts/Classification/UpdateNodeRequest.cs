namespace Meezan.WebApi.Contracts.Classification;

public sealed class UpdateNodeRequest
{
    public string? Slug { get; set; }
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsVisible { get; set; }
    public bool? RequiresChildSelection { get; set; }
    public string? ChildrenLabelAr { get; set; }
    public string? ChildrenLabelEn { get; set; }
    public string? AutoRuleJson { get; set; }
    public bool? AutoClassificationEnabled { get; set; }
}
