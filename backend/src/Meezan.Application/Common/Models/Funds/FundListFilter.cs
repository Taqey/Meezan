namespace Meezan.Application.Common.Models.Funds;

public class FundListFilter
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? RiskLevel { get; set; }
    public long? AssetManagerId { get; set; }
    public bool? IsIslamic { get; set; }
    public bool? IsFeatured { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinManagementFee { get; set; }
    public decimal? MaxManagementFee { get; set; }
    public decimal? MinInvestment { get; set; }
    public decimal? MaxInvestment { get; set; }
    public string? SortBy { get; set; }
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
