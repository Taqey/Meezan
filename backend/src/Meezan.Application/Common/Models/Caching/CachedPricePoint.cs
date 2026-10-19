namespace Meezan.Application.Common.Models.Caching;

public sealed class CachedPricePoint
{
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? Change { get; set; }
    public decimal? Performance { get; set; }
}
