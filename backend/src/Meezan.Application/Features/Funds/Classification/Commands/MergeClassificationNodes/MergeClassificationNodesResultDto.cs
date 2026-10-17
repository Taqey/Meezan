namespace Meezan.Application.Features.Funds.Classification.Commands.MergeClassificationNodes;

public sealed class MergeClassificationNodesResultDto
{
    public string Message { get; set; } = "Nodes merged";
    public int MovedAssignments { get; set; }
    public int MovedChildren { get; set; }
}
