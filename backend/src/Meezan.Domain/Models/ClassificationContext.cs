using Meezan.Domain.Entities;

namespace Meezan.Domain.Models;

public sealed class ClassificationContext
{
    public List<FundClassificationNode> Nodes { get; set; } = new();
    public Dictionary<int, FundClassificationNode> ById { get; set; } = new();
    public Dictionary<string, FundClassificationNode> BySlug { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, List<FundClassificationOverride>> OverridesByFund { get; set; } = new();
    public bool AutoEnabled { get; set; } = true;
}
