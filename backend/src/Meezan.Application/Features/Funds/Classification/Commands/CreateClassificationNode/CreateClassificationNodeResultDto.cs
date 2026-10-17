using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;

public sealed class CreateClassificationNodeResultDto
{
    public AdminClassificationNodeDto Node { get; set; } = new();
    public int MovedFunds { get; set; }
}
