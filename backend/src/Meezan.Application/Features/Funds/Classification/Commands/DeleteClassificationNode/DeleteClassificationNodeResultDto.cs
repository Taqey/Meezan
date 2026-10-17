namespace Meezan.Application.Features.Funds.Classification.Commands.DeleteClassificationNode;

public sealed class DeleteClassificationNodeResultDto
{
    public string Message { get; set; } = "Node deleted";
    public int MovedFunds { get; set; }
    public int MovedChildren { get; set; }
}
