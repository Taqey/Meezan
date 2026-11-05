namespace Meezan.Application.Features.Funds.Details.Dtos;

public sealed class FundPricePointDto
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? Change { get; set; }
    public decimal? Performance { get; set; }
}
