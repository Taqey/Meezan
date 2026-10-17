namespace Meezan.Domain.Entities;

public class StockSupportResistance
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? ChangePct { get; set; }
    public decimal? Pivot { get; set; }
    public decimal? R1 { get; set; }
    public decimal? R2 { get; set; }
    public decimal? S1 { get; set; }
    public decimal? S2 { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
}
