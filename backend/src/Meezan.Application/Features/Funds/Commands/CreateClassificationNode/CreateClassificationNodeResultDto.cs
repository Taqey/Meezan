using Meezan.Application.Features.Funds.Common;

namespace Meezan.Application.Features.Funds.Commands.CreateClassificationNode;

public sealed class CreateClassificationNodeResultDto
{
    public AdminClassificationNodeDto Node { get; set; } = new();
    public int MovedFunds { get; set; }
}
