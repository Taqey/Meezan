namespace Meezan.Application.Features.Funds.Classification.Commands.AssignClassificationFund;

public sealed class AssignClassificationFundResultDto
{
    public string Message { get; set; } = "Fund assigned";
    public int FundId { get; set; }
    public int NodeId { get; set; }
}
