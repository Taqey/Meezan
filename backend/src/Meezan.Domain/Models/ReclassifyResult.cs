namespace Meezan.Domain.Models;

public sealed class ReclassifyResult
{
    public int Processed { get; set; }
    public int Moved { get; set; }
    public int Remaining { get; set; }
    public int Failed { get; set; }
    public List<string> MovedCodes { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
