using Meezan.Application.Features.Funds.Classification.Dtos;

namespace Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;

public sealed class CreateClassificationNodeResultDto
{
    public AdminClassificationNodeDto Node { get; set; } = new();
    public int MovedFunds { get; set; }
}
