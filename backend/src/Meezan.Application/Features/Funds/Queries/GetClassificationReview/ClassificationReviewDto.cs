using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationReview;

public sealed class ClassificationReviewDto
{
    public int UnclassifiedCount { get; set; }
    public int ConflictingCount { get; set; }
    public int MissingChildCount { get; set; }
    public List<ReviewRow> UnclassifiedEquity { get; set; } = new();
    public List<ReviewRow> Conflicting { get; set; } = new();
    public List<ReviewRow> MissingChild { get; set; } = new();
}
