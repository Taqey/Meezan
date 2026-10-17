namespace Meezan.Application.Features.Funds.Classification.Commands.ResetClassificationFundAssignment;

public sealed class ResetClassificationFundAssignmentResultDto
{
    public string Message { get; set; } = "Fund reset to auto";
    public int ClearedCount { get; set; }
}
