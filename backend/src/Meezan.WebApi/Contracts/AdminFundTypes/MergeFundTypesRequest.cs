namespace Meezan.WebApi.Contracts.AdminFundTypes;

public sealed class MergeFundTypesRequest
{
    public int SourceId { get; set; }
    public int TargetId { get; set; }
}
