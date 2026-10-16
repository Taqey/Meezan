namespace Meezan.Application.Features.Funds.Common;

public sealed class AdminNodeFundRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string AssetManagerName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool NeedsReview { get; set; }
    public bool IsManual { get; set; }
}
