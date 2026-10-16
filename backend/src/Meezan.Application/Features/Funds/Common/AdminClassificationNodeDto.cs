namespace Meezan.Application.Features.Funds.Common;

public sealed class AdminClassificationNodeDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Level { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedReason { get; set; } = string.Empty;
    public bool RequiresChildSelection { get; set; }
    public string ChildrenLabelAr { get; set; } = string.Empty;
    public string ChildrenLabelEn { get; set; } = string.Empty;
    public string AutoRuleJson { get; set; } = string.Empty;
    public bool AutoClassificationEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public int DirectCount { get; set; }
    public int TotalCount { get; set; }
}
