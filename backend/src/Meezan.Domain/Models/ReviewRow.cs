namespace Meezan.Domain.Models;

public sealed class ReviewRow
{
    public int FundId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string AssetManagerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? InvestmentStrategy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsManual { get; set; }
}
