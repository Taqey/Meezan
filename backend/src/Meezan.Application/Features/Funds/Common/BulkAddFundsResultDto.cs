namespace Meezan.Application.Features.Funds.Common;

public sealed class BulkAddFundsResultDto
{
    public int Added { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
