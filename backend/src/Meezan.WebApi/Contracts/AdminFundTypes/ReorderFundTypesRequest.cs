namespace Meezan.WebApi.Contracts.AdminFundTypes;

public sealed class ReorderFundTypesRequest
{
    public List<int>? OrderedIds { get; set; }
}
