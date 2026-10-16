namespace Meezan.WebApi.Contracts.Classification;

public sealed class AssignFundRequest
{
    public int NodeId { get; set; }
    public string? Reason { get; set; }
}
