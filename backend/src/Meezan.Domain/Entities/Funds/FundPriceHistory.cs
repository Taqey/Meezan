namespace Meezan.Domain.Entities;

/// <summary>
/// Fund price history point for charts.
/// </summary>
public class FundPriceHistory
{
    public int Id { get; set; }
    public int FundId { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? Change { get; set; }
    public decimal? Performance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Fund? Fund { get; set; }
}
