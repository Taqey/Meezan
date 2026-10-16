namespace Meezan.Application.Features.Funds.Common;

public sealed class FundStatsDto
{
    public int TotalCount { get; set; }
    public int IslamicCount { get; set; }
    public double TotalAum { get; set; }
    public double? BestDailyChange { get; set; }
    public string? BestDailyChangeCode { get; set; }
}
