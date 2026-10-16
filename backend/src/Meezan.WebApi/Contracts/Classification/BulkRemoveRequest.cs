namespace Meezan.WebApi.Contracts.Classification;

public sealed class BulkRemoveRequest
{
    public List<int>? FundIds { get; set; }
    public int? DestinationNodeId { get; set; }
    public bool ResetToAuto { get; set; }
}
