namespace Meezan.Domain.Models;

public sealed class ReviewQueue
{
    public List<ReviewRow> UnclassifiedEquity { get; set; } = new();
    public List<ReviewRow> Conflicting { get; set; } = new();
    public List<ReviewRow> MissingChild { get; set; } = new();
}
