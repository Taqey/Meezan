namespace Meezan.Domain.Entities;

/// <summary>
/// Daily stock snapshot recorded at each scrape pull to enable exact historical recalculations.
/// </summary>
public class StockDailySnapshot
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public DateOnly TradingDate { get; set; }

    public decimal? Price { get; set; }
    public decimal? PrevClose { get; set; }
    public decimal? ChangePct { get; set; }
    public decimal? SharesUsed { get; set; }
    public string? Currency { get; set; }
    public int? SectorId { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
    public Sector? Sector { get; set; }
}
