using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Queries.GetClassificationDeletePreview;

public sealed class ClassificationDeletePreviewDto
{
    public AdminNodeBriefDto Node { get; set; } = new();
    public int DirectFunds { get; set; }
    public int DescendantFunds { get; set; }
    public int ChildNodes { get; set; }
}
