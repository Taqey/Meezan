namespace Meezan.Application.Features.Funds.Common;

public sealed class BulkRemoveFundsResultDto
{
    public int Moved { get; set; }
    public int Reset { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
