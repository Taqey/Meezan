namespace Meezan.WebApi.Contracts.RemovalCandidates;

public sealed class RemovalCandidatesSelectionRequest
{
    public List<string> Tickers { get; set; } = new();
    public string? Reason { get; set; }
}
