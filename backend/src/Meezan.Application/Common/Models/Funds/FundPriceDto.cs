namespace Meezan.Application.Common.Models.Funds;

public sealed class FundPriceDto
{
    public int Id { get; set; }
    public decimal? CurrentPrice { get; set; }
    public decimal? DailyChange { get; set; }
    public decimal? Return1M { get; set; }
    public decimal? Return3M { get; set; }
    public decimal? Return6M { get; set; }
    public decimal? Return1Y { get; set; }
    public decimal? Return3Y { get; set; }
    public DateTime? PricesUpdatedAt { get; set; }
}
