namespace Meezan.Domain.Models;

public sealed class ChangedFund
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OldPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
