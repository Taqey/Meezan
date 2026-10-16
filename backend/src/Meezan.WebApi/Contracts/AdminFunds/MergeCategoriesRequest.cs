namespace Meezan.WebApi.Contracts.AdminFunds;

public sealed class MergeCategoriesRequest
{
    public int SourceCategoryId { get; set; }
    public int TargetCategoryId { get; set; }
}
