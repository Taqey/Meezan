namespace Meezan.Domain.Entities;

public class StockMarketData
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public decimal? NominalValue { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? BookValue { get; set; }
    public decimal? PbRatio { get; set; }
    public decimal? Eps { get; set; }
    public decimal? PeRatio { get; set; }
    public string? Currency { get; set; }
    public decimal? High { get; set; }
    public decimal? Low { get; set; }
    public decimal? Open { get; set; }
    public decimal? ClosingPrice { get; set; }
    public string? SourceLastUpdateText { get; set; }
    /// <summary>Timestamp of the last daily live-price scrape run.</summary>
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Timestamp of the last quarterly fundamentals scrape run.
    /// Null until the quarterly job has run at least once.
    /// </summary>
    public DateTime? SlowDataFetchedAt { get; set; }

    public Stock? Stock { get; set; }
}
