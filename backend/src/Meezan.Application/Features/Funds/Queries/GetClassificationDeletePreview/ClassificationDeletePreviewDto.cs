using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Queries.GetClassificationDeletePreview;

public sealed class ClassificationDeletePreviewDto
{
    public AdminNodeBriefDto Node { get; set; } = new();
    public int DirectFunds { get; set; }
    public int DescendantFunds { get; set; }
    public int ChildNodes { get; set; }
}
